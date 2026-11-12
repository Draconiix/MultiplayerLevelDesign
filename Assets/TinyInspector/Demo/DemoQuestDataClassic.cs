using System.Collections.Generic;
using UnityEngine;

namespace TinyInspector.Demo
{
    public class DemoQuestDataClassic : MonoBehaviour
    {
        [Header("Identity")]
        public string questId;
        public string title;

        [TextArea(3, 6)]
        public string description;

        [Header("Category & State")]
        public QuestCategory category;
        public QuestState initialState;

        [Header("Objectives")]
        public List<QuestObjective> objectives;

        [Header("Start Conditions")]
        public List<QuestCondition> startConditions;

        [Header("Complete Conditions")]
        public List<QuestCondition> completeConditions;

        [Header("Rewards")]
        public List<QuestReward> rewards;

        [Header("Flags")]
        public bool autoStart;
        public bool autoComplete;
    }

    public enum QuestCategory
    {
        Main,
        Side,
        Daily,
        Event
    }

    public enum QuestState
    {
        Locked,
        Available,
        Active
    }

    [System.Serializable]
    public class QuestObjective
    {
        public ObjectiveType type;

        [TextArea(2, 4)]
        public string description;

        public string targetId;
        public int requiredAmount;

        public bool optional;
    }

    public enum ObjectiveType
    {
        Kill,
        Collect,
        Interact,
        ReachLocation
    }

    [System.Serializable]
    public class QuestCondition
    {
        public ConditionType type;

        public string parameter;
        public int value;
    }

    public enum ConditionType
    {
        Level,
        QuestCompleted,
        HasItem,
        HasTag
    }

    [System.Serializable]
    public class QuestReward
    {
        public RewardType type;

        public int amount;
        public GameObject item;
    }

    public enum RewardType
    {
        Gold,
        Experience,
        Item
    }
}
