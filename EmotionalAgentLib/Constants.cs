using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace EmotionalAgentLib
{
    public class Constants
    {
        public const float EmotionalInfluenceThreshhold = 0.01f; // High sensitivity to produce constant reactions, could be raised up to 0.15f
        public const float MemoryModifierStandardDivider = 10f;
        public const float DefaultEmotionalPriorityFactor = 0.5f;
        public const float ReactionReputationModifier = 0.5f;

        // Default actions
        public const string ActionAttack = "Attack";
        public const string ActionTalk = "Talk";
        public const string ActionPraise = "Praise";
        public const string ActionReprimand = "Reprimand";
        public const string ActionBeRude= "Be Rude";

        // OCC Emotions
        public const string Happiness = "Happiness";
        public const string Gloating = "Gloating";
        public const string Hope = "Hope";
        public const string Joy = "Joy";
        public const string Pride = "Pride";
        public const string Admiration = "Admiration";
        public const string Love = "Love";
        public const string Satisfaction = "Satisfaction";
        public const string Relief = "Relief";
        public const string Gratification = "Gratification";
        public const string Gratitude = "Gratitude";
    }

    public enum PersonalityType
    {
        // OCEAN
        Agreeableness,
        Conscientiousness,
        Extraversion,
        Neuroticism,
        Openness
    }

    public enum EmotionType
    {
        None,
        GoalsRelated,
        ActionsRelated,
        AttitudesRelated
    }

    public enum EmotionBipole
    {
        Joy = 1,
        Distress = -1,
        Hope = 2,
        Fear = -2,
        HappinessFor = 3,
        Resentment = -3,
        Gloating = 4,
        Pity = -4,
        Pride = 5,
        Shame = -5,
        Admiration = 6,
        Reproach = -6,
        Love = 7,
        Hatred = -7,
        Satisfaction = 8,
        ConfirmationOfFears = -8,
        Relief = 9,
        Disappointment = -9,
        Gratification = 10,
        Remorse = -10,
        Gratitude = 11,
        Anger = -11
    }

    public enum EmotionalNeedType
    {
        FeelSafe,
        FeelWell,
        FeelProud,
        HelpOthers // to be enriched
    }

    public enum ActionName // generic, users will be able to add their own
    {
        ActionAttack,
        ActionTalk,
        ActionPraise,
        ActionReprimand,
        ActionBeRude
    }

    public enum EventName // generic, users will be able to add their own
    {
        AttackEvent,
        TalkEvent,
        PraiseEvent,
        ReprimandEvent,
        BeRudeEvent
    }

    public enum EnvironmentClassification
    {
        Friendly,
        Positive,
        Neutral,
        Negative,
        Hostile   
    }

    public enum ActionType
    {
        Action,
        Reaction,
        Both
    }
}
