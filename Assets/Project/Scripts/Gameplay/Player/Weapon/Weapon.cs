using System;
using UnityEngine;
using LevelDesign.Data;
using LevelDesign.Async.Auth;
using LevelDesign.Async;
using LevelDesign.Tags;
using LevelDesign.Systems.Player;
using TinyInspector;

namespace LevelDesign.Systems.Player
{
    public class Weapon : _InputAuth
    {
        [BoxGroup("Scene Refs")]
        [SerializeField] private WeaponDataSO data;
        [BoxGroup("Scene Refs")]
        [SerializeField] private Transform viewModel;
        [BoxGroup("Scene Refs")]
        [SerializeField] private ArmController armController;
        [BoxGroup("Scene Refs")]
        [SerializeField] private Transform aimOrigin;

        [BoxGroup("Damage")]
        [SerializeField] private float damage = 10f;
        [BoxGroup("Damage")]
        [SerializeField] private float range = 200f;
        [BoxGroup("Damage")]
        [SerializeField] private LayerMask hitMask = ~0;

        [BoxGroup("FX")]
        [SerializeField] private GameObject muzzleFlashPrefab;
        [BoxGroup("FX")]
        [SerializeField] private Transform muzzle;
        [BoxGroup("FX")]
        [SerializeField] private GameObject impactPrefab;

        [BoxGroup("Debug")]
        public Player player => armController.player;

        const int BurstCount = 3;
        const float TwoPi = Mathf.PI * 2f;

        public event Action Fired;

        public WeaponDataSO Data => data;
        public float CurrentAmmo { get; private set; }

        ViewModelMovementSO Motion => data.viewModelMove;

        // Input
        bool triggerHeld;
        bool triggerPressed;
        Vector2 lookInput;
        float moveAmount;

        // Movement state
        _MovementController movementSource;
        Vector3 velocity;
        Stance stance = Stance.Stand;
        float airborneVelY;

        // Firing
        float nextFireTime;
        float lastShotTime = float.NegativeInfinity;
        int shotIndex;
        int burstRemaining;
        RaycastHit[] hitBuffer = new RaycastHit[16];
        
        // Reload
        bool isReloading;
        float reloadStartTime;

        // View model rest pose
        Vector3 restPos;
        Quaternion restRot;

        // Sway
        Vector3 swayPos;
        Vector3 swayRot;

        // Bob & breathing
        float bobTime;
        float bobBlend;
        float bobWeight = 1f;
        float bobBlendOut = 12f;

        // Air tilt
        float airTilt;

        // Landing
        float landTarget, landCurrent;

        // Weapon kick
        float kickTarget, kickCurrent;

        // Camera recoil
        Transform recoilPivot;
        Vector2 recoilTarget, recoilCurrent;

        private AnimatorTriggerSync triggerSync;

        #region Public API
        public void SetTrigger(bool held)
        {
            if(held && !triggerHeld) { triggerPressed = true; }
            triggerHeld = held;
        }

        public void SetLook(Vector2 look) => lookInput = look;

        /// 0 = standing still, 1 = full move speed. Overwritten each frame if a movement source is bound.
        public void SetMovement(float amount) => moveAmount = Mathf.Clamp01(amount);
        public void BindMovement(_MovementController source) => movementSource = source;
        public void SetMotionState(Vector3 worldVelocity, Stance newStance, float referenceSpeed = 0f)
        {
            if(!data || !data.viewModelMove) { return; }
            var t_m = Motion;

            if(stance == Stance.Air && newStance != Stance.Air) {
                float t_impact = Mathf.Max(0f, -airborneVelY);
                landTarget = Mathf.Max(landTarget, Mathf.Clamp01(t_impact * t_m.landKickPerSpeed));
            }

            if(newStance == Stance.Air) {
                airborneVelY = stance == Stance.Air ? Mathf.Min(airborneVelY, worldVelocity.y) : worldVelocity.y;
            }

            velocity = worldVelocity;
            stance = newStance;

            float t_ref = referenceSpeed > 0f ? referenceSpeed : t_m.bobReferenceSpeed;
            bool t_canBob = (newStance == Stance.Stand || newStance == Stance.Crouch) && t_ref > 0f;
            float t_horizSpeed = new Vector2(worldVelocity.x, worldVelocity.z).magnitude;
            moveAmount = t_canBob
                ? Mathf.Clamp(t_horizSpeed / t_ref, 0f, Mathf.Max(1f, t_m.bobMaxScale))
                : 0f;
        }

        // The Spaghetti Here is top 10 Worst for me, it's almost a celebration
        public void Reload()
        {
            if(isReloading) { return; }
            if(CurrentAmmo >= data.maxAmmo) { return; }
            if(!HasReserveAmmo) { return; }

            armController.armAnimator.SetTrigger("Reload");
            reloadStartTime = Time.time;

            player.characterDataM.currentMovementController._GetCurrentRigInfo().characterAnimator.SetTrigger("Reload");
            
            if(!triggerSync) { 
                triggerSync = player.characterDataM.currentMovementController.GetComponentInChildren<AnimatorTriggerSync>();
            }
            if(triggerSync) { 
                triggerSync.SendTrigger("Reload");
            }

            isReloading = true;
        }
        #endregion

        #region Unity
        void Awake()
        {
            if(!data || !data.viewModelMove) {
                Debug.LogError("Weapon needs a WeaponData with a ViewModelMovement assigned.", this);
                enabled = false;
                return;
            }

            if(!viewModel) { 
                viewModel = transform;
            }

            var t_recoilTag = GetComponentInParent<TAG_RecoilTarget>();
            if(t_recoilTag) { recoilPivot = t_recoilTag.transform; }
            else { Debug.LogWarning("Weapon found no TAG_RecoilTarget in its parents. Camera recoil is disabled.", this); }

            aInputInit(autoPopulateGame: true);
            InputAuthManager.Instance.RequestInput(this);

            restPos = viewModel.localPosition;
            restRot = viewModel.localRotation;
            CurrentAmmo = data.maxAmmo;
        }

        void OnDisable()
        {
            triggerHeld = false;
            triggerPressed = false;
            burstRemaining = 0;

            InputAuthManager.Instance.RelinquishRequest(this);
        }

        void Update()
        {
            float t_dt = Time.deltaTime;

            if(movementSource) {
                SetMotionState(movementSource._GetVelocity(), movementSource._stanceMirror, movementSource._GetWalkSpeed());
            }

            if(isReloading && (Time.time >= reloadStartTime + data.reloadTime))
            {
                FinishReload();
            }

            HandleFiring();
            UpdateCameraRecoil(t_dt);
            UpdateViewModel(t_dt);

            UpdateGUI();

            if(!_inputAuthorized) { return; }
            SetTrigger(_input.Fire.IsPressed());
            SetLook(_input.Look.ReadValue<Vector2>());
            if(_input.Reload.WasPressedThisFrame()) { Reload(); }
        }
        #endregion

        void UpdateGUI()
        {
            StackManager stacks = Stacks;
            string reserve;

            if(stacks == null) {
                reserve = data.maxAmmo.ToString();
            } else if(stacks.IsUnlimited(StackType.Ammo)) {
                reserve = "∞";
            } else {
                reserve = stacks.Get(StackType.Ammo).ToString();
            }

            player.uiM.ammoText.text = CurrentAmmo.ToString() + "/" + data.maxAmmo;
        }

        #region Ammo
        StackManager Stacks => armController != null && armController.player != null ? armController.player.StackM : null;
        bool HasReserveAmmo => Stacks == null || Stacks.Has(StackType.Ammo);

        public int ReserveMags => Stacks != null ? Stacks.Get(StackType.Ammo) : 0;
        public bool IsReloading => isReloading;

        // Each Ammo stack is one spare magazine. Without a StackManager reloads are free, so the weapon still works outside a Player.
        void FinishReload() {
            isReloading = false;

            StackManager stacks = Stacks;
            if(stacks != null && !stacks.TryConsume(StackType.Ammo)) { return; }

            CurrentAmmo = data.maxAmmo;
        }
        #endregion

        #region Firing
        void HandleFiring()
        {            
            if(Time.time < nextFireTime) { return; }
            if(isReloading) { return; }

            if(burstRemaining > 0) {
                burstRemaining--;
                Shoot();
                return;
            }

            bool t_wantsShot = data.fireMode == WeaponDataSO.FireMode.Auto ? triggerHeld : triggerPressed;
            if(!t_wantsShot) { return; }

            triggerPressed = false;
            if(data.fireMode == WeaponDataSO.FireMode.Burst) { burstRemaining = BurstCount - 1; }
            Shoot();
        }

        void Shoot()
        {
            nextFireTime = Time.time + 1f / Mathf.Max(data.fireRate, 0.01f);

            if(CurrentAmmo < data.ammoConsumedPerTick) {
                burstRemaining = 0;
                Reload();
                return;
            }

            CurrentAmmo -= data.ammoConsumedPerTick;

            if(Time.time - lastShotTime > Motion.sprayResetTime) { shotIndex = 0; }
            lastShotTime = Time.time;

            recoilTarget += Motion.GetRecoil(shotIndex++);
            kickTarget = 1f;

            if(muzzleFlashPrefab) {
                Instantiate(muzzleFlashPrefab, muzzle ? muzzle : viewModel);
            }

            ApplyHit();

            Fired?.Invoke();
        }

        // The closest hit that isn't the shooter wins, so walls and props block shots before they reach a Hurtbox.
        void ApplyHit() {
            Transform origin = aimOrigin ? aimOrigin : recoilPivot;
            if(!origin) { return; }

            Player shooter = armController != null ? armController.player : null;
            int hitCount = Physics.RaycastNonAlloc(origin.position, origin.forward, hitBuffer, range, hitMask, QueryTriggerInteraction.Collide);

            Collider closest = null;
            RaycastHit closestHit = default;
            float closestDistance = float.MaxValue;

            for(int i = 0; i < hitCount; i++) {
                RaycastHit hit = hitBuffer[i];
                bool isShooter = shooter && hit.collider.GetComponentInParent<Player>() == shooter;

                if(!isShooter && hit.distance < closestDistance) {
                    closest = hit.collider;
                    closestHit = hit;
                    closestDistance = hit.distance;
                }
            }

            if(!closest) { return; }

            SpawnImpact(closestHit);

            Hurtbox hurtbox = closest.GetComponentInParent<Hurtbox>();
            if(!hurtbox) { return; }

            hurtbox.Trigger(damage, true);
            player.uiM.GameplayHit();
        }

        void SpawnImpact(RaycastHit hit) {
            if(!impactPrefab) { return; }

            Instantiate(impactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
        }
        #endregion

        #region Camera Recoil
        void UpdateCameraRecoil(float t_dt)
        {
            var t_m = Motion;

            recoilTarget = Vector2.Lerp(recoilTarget, Vector2.zero, Damp(t_m.recoilReturn, t_dt));
            recoilCurrent = Vector2.Lerp(recoilCurrent, recoilTarget, Damp(t_m.recoilSnappiness, t_dt));

            if(recoilPivot) {
                recoilPivot.localRotation = Quaternion.Euler(-recoilCurrent.y, recoilCurrent.x, 0f);
            }
        }
        #endregion

        #region View Model
        void UpdateViewModel(float t_dt)
        {
            var t_m = Motion;

            UpdateSway(t_dt);
            UpdateBobAndBreath(t_dt, out Vector3 t_bobPos, out Vector3 t_bobRot);
            UpdateKick(t_dt);
            UpdateAirTilt(t_dt);
            UpdateLanding(t_dt);

            Vector3 t_kickPos = Vector3.back * (kickCurrent * t_m.kickBack);
            float t_kickPitch = -kickCurrent * t_m.kickPitch;

            float t_air01 = t_m.airTiltMax > 0f ? airTilt / t_m.airTiltMax : 0f;
            Vector3 t_airPos = Vector3.down * (t_air01 * t_m.airOffset);

            Vector3 t_landPos = Vector3.down * (landCurrent * t_m.landDrop);
            float t_landPitch = landCurrent * t_m.landPitch;

            Vector3 t_rot = swayRot + t_bobRot;
            t_rot.x += t_kickPitch + airTilt + t_landPitch;

            viewModel.localPosition = restPos + swayPos + t_bobPos + t_kickPos + t_airPos + t_landPos;
            viewModel.localRotation = restRot * Quaternion.Euler(t_rot);
        }

        void UpdateSway(float t_dt)
        {
            var t_m = Motion;
            Vector2 t_look = Vector2.ClampMagnitude(lookInput * t_m.swayInputScale, 1f);

            Vector3 t_pos = new Vector3(-t_look.x, -t_look.y, 0f) * t_m.swayAmount;
            Vector3 t_rot = new Vector3(t_look.y, -t_look.x, -t_look.x) * t_m.swayTilt;

            float t_smooth = Damp(t_m.swaySmooth, t_dt);
            swayPos = Vector3.Lerp(swayPos, t_pos, t_smooth);
            swayRot = Vector3.Lerp(swayRot, t_rot, t_smooth);
        }

        void UpdateBobAndBreath(float dt, out Vector3 pos, out Vector3 rot) {
            var m = Motion;
            bobBlend = Mathf.Lerp(bobBlend, moveAmount, Damp(m.bobBlendSpeed, dt));
            bobWeight = Mathf.Lerp(bobWeight, triggerHeld ? 0f : 1f, Damp(bobBlendOut, dt));

            bobTime += dt * m.bobSpeed * bobBlend * TwoPi;
            if(bobTime > TwoPi) {
                bobTime -= TwoPi;
            }

            float bob = Mathf.Sin(bobTime);
            float dip = Mathf.Abs(bob);

            pos = new Vector3(bob, -dip, 0f) * (m.bobAmount * bobBlend);
            rot = new Vector3(dip * m.bobPitch, bob * m.bobYaw, -bob * m.bobRoll) * bobBlend;
            pos.y += Mathf.Sin(Time.time * m.breathSpeed * TwoPi) * m.breathAmount * Mathf.Clamp01(1f - bobBlend);

            pos *= bobWeight;
            rot *= bobWeight;
        }

        void UpdateAirTilt(float t_dt)
        {
            var t_m = Motion;
            float t_target = stance == Stance.Air
                ? Mathf.Clamp(velocity.y * t_m.airTiltPerSpeed, -t_m.airTiltMax, t_m.airTiltMax)
                : 0f;
            airTilt = Mathf.Lerp(airTilt, t_target, Damp(t_m.airTiltSmooth, t_dt));
        }

        void UpdateLanding(float t_dt)
        {
            var t_m = Motion;
            landTarget = Mathf.Lerp(landTarget, 0f, Damp(t_m.landReturn, t_dt));
            landCurrent = Mathf.Lerp(landCurrent, landTarget, Damp(t_m.landSnappiness, t_dt));
        }

        void UpdateKick(float t_dt)
        {
            var t_m = Motion;
            kickTarget = Mathf.Lerp(kickTarget, 0f, Damp(t_m.kickReturn, t_dt));
            kickCurrent = Mathf.Lerp(kickCurrent, kickTarget, Damp(t_m.kickSnappiness, t_dt));
        }
        #endregion

        static float Damp(float t_speed, float t_dt) => 1f - Mathf.Exp(-t_speed * t_dt);
    }
}