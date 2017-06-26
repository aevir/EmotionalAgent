using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using EmotionalAgentLib;
using Action = EmotionalAgentLib.Action;

// A repository for default Actions, Personalities, RationalStates & Emotions
namespace MainLibrary
{
    public class ActionsLibrary
    {
        public static List<Action> AvailableActions { get; }

        static ActionsLibrary()
        {
            AvailableActions = new List<Action>();
        }

        public static void InitializeTestActions()
        {
            SetTestActions();
        }

        public static void AddCustomAction(Action action)
        {
            AvailableActions.Add(action);
        }

        private static void SetTestActions()
        {
            List<Event> standardActionEvents = new List<Event>()
            {
                new EmotionalAgentLib.Event()
                {
                    Name = Constants.ActionAttack,
                    EmotionalInfluence = new Dictionary<EmotionBipole, float>()
                    {
                        {EmotionBipole.Joy, -4},
                        {EmotionBipole.Hope, -2 },
                        {EmotionBipole.Love, -4 }
                    }                    
                },
                new EmotionalAgentLib.Event()
                {
                    Name = Constants.ActionTalk,
                    EmotionalInfluence = new Dictionary<EmotionBipole, float>()
                    {
                        {EmotionBipole.Satisfaction, 0.1f},
                        {EmotionBipole.Gratification, 0.1f },
                    }
                },
                new EmotionalAgentLib.Event()
                {
                    Name = Constants.ActionPraise,
                    EmotionalInfluence = new Dictionary<EmotionBipole, float>()
                    {
                        {EmotionBipole.Satisfaction, 1f},
                        {EmotionBipole.Relief, 1f },
                        {EmotionBipole.Gloating, 1f }
                    }
                },
                new EmotionalAgentLib.Event()
                {
                    Name = Constants.ActionReprimand,
                    EmotionalInfluence = new Dictionary<EmotionBipole, float>()
                    {
                        {EmotionBipole.Satisfaction, -1f},
                        {EmotionBipole.Relief, -1f },
                        {EmotionBipole.Gloating, -1f }
                    }
                },
                new EmotionalAgentLib.Event()
                {
                    Name = Constants.ActionBeRude,
                    EmotionalInfluence = new Dictionary<EmotionBipole, float>()
                    {
                        {EmotionBipole.Satisfaction, -1f},
                        {EmotionBipole.Relief, -1f },
                        {EmotionBipole.Gloating, -1f }
                    }
                }
            };
            List<Action> standardActions = new List<Action>()
            {
                new Action(0, Constants.ActionAttack, ActionType.Both, -1f, standardActionEvents.First(e => e.Name == Constants.ActionAttack), 1f, new Dictionary<EmotionType, float>() { {EmotionType.ActionsRelated, 1f}}),
                new Action(1, Constants.ActionTalk, ActionType.Both, 0f, standardActionEvents.First(e => e.Name == Constants.ActionTalk), 1f, new Dictionary<EmotionType, float>() { {EmotionType.GoalsRelated, 0.75f}, {EmotionType.AttitudesRelated, 0.25f} }),
                new Action(2, Constants.ActionPraise, ActionType.Action, 1f, standardActionEvents.First(e => e.Name == Constants.ActionPraise), 1f, new Dictionary<EmotionType, float>() { {EmotionType.GoalsRelated, 0.5f}, {EmotionType.AttitudesRelated, 0.5f} }),
                new Action(3, Constants.ActionReprimand, ActionType.Action, -1f, standardActionEvents.First(e => e.Name == Constants.ActionReprimand), 1f, new Dictionary<EmotionType, float>() {{EmotionType.GoalsRelated, 0.5f}, {EmotionType.AttitudesRelated, 0.5f}}),
                new Action(4, Constants.ActionBeRude, ActionType.Action, -1f, standardActionEvents.First(e => e.Name == Constants.ActionBeRude), 1f, new Dictionary<EmotionType, float>() {{EmotionType.GoalsRelated, 0.5f}, {EmotionType.AttitudesRelated, 0.5f}})
            };
            AvailableActions.AddRange(standardActions);
        }
    }

    public class PersonalityLibrary
    {
        public static List<Personality> Personalities; 
        public static Personality DefaultPersonality = new Personality
            (new Dictionary<PersonalityType, float>()
            {
                {PersonalityType.Agreeableness, 0.5f},
                {PersonalityType.Conscientiousness, 0.5f},
                {PersonalityType.Extraversion, 0.5f},
                {PersonalityType.Neuroticism, 0.5f},
                {PersonalityType.Openness, 0.5f}
            });

        public PersonalityLibrary()
        {
            Personalities = new List<Personality>();
            Personalities.Add(DefaultPersonality);
        }
    }

    // Using the OCC model of Emotions
    public class EmotionsLibrary
    {
        // ARBITRARY
        public static Emotion HappyFor = new Emotion(Constants.Happiness, EmotionType.AttitudesRelated, false, new Tuple<EmotionBipole, float>(EmotionBipole.HappinessFor, 0.5f), new Tuple<EmotionBipole, float>(EmotionBipole.Resentment, 0.5f));
        public static Emotion Gloating = new Emotion(Constants.Gloating, EmotionType.GoalsRelated, false, new Tuple<EmotionBipole, float>(EmotionBipole.Gloating, 0.5f), new Tuple<EmotionBipole, float>(EmotionBipole.Pity, 0.5f));
        public static Emotion Hope = new Emotion(Constants.Hope, EmotionType.ActionsRelated, true, new Tuple<EmotionBipole, float>(EmotionBipole.Hope, 0.5f), new Tuple<EmotionBipole, float>(EmotionBipole.Fear, 0.5f));
        public static Emotion Joy = new Emotion(Constants.Joy, EmotionType.ActionsRelated, true, new Tuple<EmotionBipole, float>(EmotionBipole.Joy, 0.5f), new Tuple<EmotionBipole, float>(EmotionBipole.Distress, 0.5f));
        public static Emotion Pride = new Emotion(Constants.Pride, EmotionType.AttitudesRelated, true, new Tuple<EmotionBipole, float>(EmotionBipole.Pride, 0.5f), new Tuple<EmotionBipole, float>(EmotionBipole.Shame, 0.5f));
        public static Emotion Admiration = new Emotion(Constants.Admiration, EmotionType.AttitudesRelated, false, new Tuple<EmotionBipole, float>(EmotionBipole.Admiration, 0.5f), new Tuple<EmotionBipole, float>(EmotionBipole.Reproach, 0.5f));
        public static Emotion Love = new Emotion(Constants.Love, EmotionType.ActionsRelated, false, new Tuple<EmotionBipole, float>(EmotionBipole.Love, 0.5f), new Tuple<EmotionBipole, float>(EmotionBipole.Hatred, 0.5f));
        public static Emotion Satisfaction = new Emotion(Constants.Satisfaction, EmotionType.GoalsRelated, true, new Tuple<EmotionBipole, float>(EmotionBipole.Satisfaction, 0.5f), new Tuple<EmotionBipole, float>(EmotionBipole.ConfirmationOfFears, 0.5f));
        public static Emotion Relief = new Emotion(Constants.Relief, EmotionType.GoalsRelated, false, new Tuple<EmotionBipole, float>(EmotionBipole.Relief, 0.5f), new Tuple<EmotionBipole, float>(EmotionBipole.Disappointment, 0.5f));
        public static Emotion Gratification = new Emotion(Constants.Gratification, EmotionType.AttitudesRelated, true, new Tuple<EmotionBipole, float>(EmotionBipole.Gratification, 0.5f), new Tuple<EmotionBipole, float>(EmotionBipole.Remorse, 0.5f));
        public static Emotion Gratitude = new Emotion(Constants.Gratitude, EmotionType.ActionsRelated, false, new Tuple<EmotionBipole, float>(EmotionBipole.Gratitude, 0.5f), new Tuple<EmotionBipole, float>(EmotionBipole.Anger, 0.5f));

        public static List<Emotion> GetDefaultEmotionalState()
        {
            return new List<Emotion>()
            {
                HappyFor,
                Gloating,
                Hope,
                Joy,
                Pride,
                Admiration,
                Love,
                Satisfaction,
                Relief,
                Gratification,
                Gratitude
            };
        }
    }

    public class RationalLibrary
    {
        public static RationalState GetDefaultRationalState()
        {
            return new RationalState();
        }
    }
}
