using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace EmotionalAgentLib
{
    // Future work: Study the relationship between PersonalityTypes & Emotions and apply the findings in Emotion.ApplyPersonality()
    /*
        Event Appraisal: Personality -> Affects appraisal of Actions -> Affects Emotions
        Emotions -> Affect decisions regarding Actions & Reactions -> Produce Events
    */
    #region Agent
    [Serializable]
    public class Agent
    {
        public int Id;
        public Personality Personality;
        public RationalState RationalState;
        public List<Emotion> EmotionalState; // A Dictionary held by the Agent of all emotions and their intensity
        public List<Action> AvailableActions; // previously named Desires, should be filled
        public Dictionary<int, List<Emotion>> ActionsMemory; // emotions caused by own actions
        public Dictionary<int, float> AgentsMemory; // reputation of other Agents by AgentId
        public bool FreeActionSelection = true;
        public bool FreeReactionSelection = true;

        public Agent(int agentId, Personality personality, RationalState rationalState, List<Emotion> emotionalState, List<Action> availableActions)
        {
            Id = agentId;
            Personality = personality;
            RationalState = rationalState;
            EmotionalState = emotionalState;
            AvailableActions = availableActions;
            // Memories should not be added at constructor level; insert by using memory modification methods instead
            ActionsMemory = new Dictionary<int, List<Emotion>>();
            AgentsMemory = new Dictionary<int, float>();
        }

        public Agent()
        {
            ActionsMemory = new Dictionary<int, List<Emotion>>();
            AgentsMemory = new Dictionary<int, float>();
        }

        public void SetEnvironment(EnvironmentClassification classification)
        {
            RationalState.ClassifyEnvironment(classification);
        }

        // Apply to perceived actions
        public Reaction ApplyPerception(Action action, Agent actionPerformer)
        {
            var outcome = action.OutcomeEvent;
            // Check the overall perceived value of the action towards the Agent's emotional & rational standards and Modify AgentsMemory accordingly
            ModifyMemory(action.PerformedBy.Id, AssessActionByEvent(outcome));

            // Appraise according to goals, well being of self & others
            var influence = PerformEmotionalAppraisal(outcome);

            // Check if the action if reaction-worthy and react if required
            return ShouldReact(action.OutcomeEvent, influence) && actionPerformer != this ? React(new List<Agent>() { actionPerformer }, action) : null;
        }

        // Appraises an action and initiates a reaction if required
        public Reaction GetActionFeedBack(Action action)
        {
            if (action == null) return null;
            Reaction affectedAgentReaction = null;
            foreach (var agent in action.AffectedAgents)
            {
                affectedAgentReaction = agent.ApplyPerception(action, action.PerformedBy);
                if (affectedAgentReaction != null)
                    action.PerformedBy.PostProcessAction(action, affectedAgentReaction);
            }
            return affectedAgentReaction;
        }

        // Perform an Action based on current status
        public Action Act(List<Agent> targetAgents, Action action = null)
        {
            // Determine an Action if the Action to perform isn't already given
            if (action != null)
            {
                action.AffectedAgents = targetAgents;
            }
            if (action == null)
            {
                action = MakeDecision(targetAgents);
            }
            action.PerformedBy = this;
            return action;
        }

        // Contemplate Actions of self based on their consequences. Should be called after the Action has taken place and its Event has been determined. Should also take into consideration the Reaction caused by that Action.
        public void PostProcessAction(Action action, Reaction reaction)
        {
            if (action.PerformedBy != this) return;

            AddToActionsMemory(action, reaction);
        }

        // Appraise according to goals, well being of self & others. Take note of the Action's performer & determine a Reaction towards specified Action.
        private Reaction React(List<Agent> targetAgents, Action action)
        {
            if (action != null)
            {
                action.AffectedAgents = targetAgents;
            }
            // Take note of the Action's performer and the environment and then determine a Reaction
            Reaction selectedReaction = MakeDecision(action);
            // Modify memories of target Agents
            foreach (var agent in targetAgents)
            {
                agent.ModifyMemory(this.Id, AssessActionByEvent(selectedReaction.OutcomeEvent), true);
            }
            return selectedReaction;
        }

        // Combine RationalState + EmotionalState to produce a Reaction from the AvailableReactions list
        private Reaction MakeDecision(Action reactionTo)
        {
            // The rationalFactor denotes how likely it is to perform a positive or negative Action in the current Environment
            float actionProbability;
            var envGrades = RationalState.GetEnvironmentClassificationGrades();
            float positiveRationalFactor = envGrades.First, negativeRationalFactor = envGrades.Second;

            float actionValue = reactionTo.OutcomeEvent.EmotionalInfluence.Values.Sum() / reactionTo.OutcomeEvent.EmotionalInfluence.Count();
            float reactionIntensity;
            float reactionPriority;
            float reactionValue;
            Dictionary<Action, float> GradedActions = new Dictionary<Action, float>();

            foreach (var reaction in AvailableActions.Where(a => a.ActionType != ActionType.Action))
            {
                actionProbability = 0.1f;
                reaction.AffectedAgents = new List<Agent>();
                reactionIntensity = reaction.IntensityValue;
                reactionPriority = reaction.ActionPriority;
                reactionValue = reaction.OutcomeEvent.EmotionalInfluence.Values.Sum() / reaction.OutcomeEvent.EmotionalInfluence.Count();

                // Selection logic
                // The selected Reaction and the Action that caused it should have equivalent intensity
                var intensityDiff = Math.Abs(reactionTo.IntensityValue - reactionIntensity);
                actionProbability += intensityDiff;

                // The selected Reaction is affected by ActionPriority (dev side variable)
                actionProbability += reactionPriority;

                // The selected Reaction should be positive or negative as the received Action and as close as possible in terms of value (limited by developer)
                if (!FreeReactionSelection && (actionValue > 0 && reactionValue < 0 || actionValue < 0 && reactionValue > 0)) continue;
                var valueDiff = Math.Abs(actionValue - reactionValue);
                actionProbability += valueDiff;

                // Also take into account Environment Classification
                actionProbability += reactionValue > 0 ? 1 - positiveRationalFactor : 1 - negativeRationalFactor;

                // Add other Agent's reputation from the Agent's memory
                if (AgentsMemory.ContainsKey(reactionTo.PerformedBy.Id))
                {
                    var actorsMemoryModifier = AgentsMemory.First(k => k.Key == reactionTo.PerformedBy.Id).Value / 10;
                    if ((actorsMemoryModifier < 0 && reactionValue < 0) || (actorsMemoryModifier >= 0 && reactionValue >= 0))
                    {
                        actorsMemoryModifier = 0;
                    }
                    actionProbability += Math.Abs(actorsMemoryModifier);
                }

                // Add emotional priorities (presence of emotions that are likely to influence the selection of a particular type of Reaction)
                actionProbability += CalculateEmotionalPriorities(reaction);

                // Future work: expectations, likelihood to select, personality priorities

                // Allow the developer to limit reactions
                if (reactionTo.AvailableReactions.Any())
                {
                    if (reactionTo.AvailableReactions.IndexOf(reaction.Name) < 0) continue;
                }
                if (reactionTo.UnavailableReactions.Any())
                {
                    if (reactionTo.UnavailableReactions.IndexOf(reaction.Name) >= 0) continue;
                }
                // Add the Reaction and its grade to the Dictionary
                GradedActions.Add(reaction, actionProbability);
            }
            Action lowestGradedAction = SelectByValuePercentage(GradedActions);
            return new Reaction(lowestGradedAction, reactionTo.Id);
        }

        // Combine RationalState + EmotionalState to produce a primary Action from the AvailableActions list
        private Action MakeDecision(List<Agent> targetAgents)
        {
            float actionProbability;
            var envGrades = RationalState.GetEnvironmentClassificationGrades();
            float positiveRationalFactor = envGrades.First, negativeRationalFactor = envGrades.Second;
            float actionIntensity;
            float actionPriority;
            float actionValue;
            Dictionary<Action, float> GradedActions = new Dictionary<Action, float>();

            foreach (var action in AvailableActions.Where(a => a.ActionType != ActionType.Reaction))
            {
                actionProbability = 0.1f;
                action.AffectedAgents = new List<Agent>();
                actionIntensity = action.IntensityValue;
                actionPriority = action.ActionPriority;
                actionValue = action.OutcomeEvent.EmotionalInfluence.Values.Sum() / action.OutcomeEvent.EmotionalInfluence.Count();

                // Logic
                // The selected Action is affected by ActionPriority (dev side variable)
                actionProbability -= actionPriority;

                // Also take into account Environment Classification
                actionProbability += actionValue > 0 ? 1 - positiveRationalFactor : 1 - negativeRationalFactor;

                // Add other Agents' reputation from the Agent's memory
                var actorsMemoryModifier = 0f;
                var actorsCount = 0;
                foreach (var id in targetAgents.Select(a => a.Id))
                {
                    if (AgentsMemory.Keys.Any(k => k == id))
                    {
                        actorsMemoryModifier += AgentsMemory[id];
                        actorsCount++;
                    }
                }
                if (!FreeActionSelection && ((actorsMemoryModifier < 0 && actionValue < 0) || (actorsMemoryModifier >= 0 && actionValue >= 0)))
                {
                    // Do not perform negative actions autonomously in first contact
                    if (actionValue < 0) continue;
                }
                else
                {
                    // Do not perform negative actions autonomously in positive relationships (developer can change that)
                    if (!FreeActionSelection && (actorsMemoryModifier > 0 && actionValue < 0 || actorsMemoryModifier < 0 && actionValue > 0)) continue;

                    // ARBITRARY
                    if (actorsCount > 0)
                        actorsMemoryModifier = Math.Abs(actorsMemoryModifier) / Constants.MemoryModifierStandardDivider / actorsCount;

                }
                actionProbability += actorsMemoryModifier;

                // Future work: Apply personality, move inside CalculateEmotionalPriorities                 
                foreach (var emotion in EmotionalState.Where(e => action.OutcomeEvent.EmotionalInfluence.ContainsKey(e.PositivePole.First)))
                {
                    //if (!action.OutcomeEvent.EmotionalInfluence.ContainsKey(emotion.PositivePole.First)) continue;
                    var prevalentPoleValue = emotion.PositivePole.Second >= emotion.NegativePole.Second
                        ? emotion.PositivePole.First
                        : emotion.NegativePole.First;
                    var factor = emotion.ApplyPersonality(emotion.PositivePole.First, Personality);
                    if ((prevalentPoleValue < 0 && actionValue < 0) || (prevalentPoleValue > 0 && actionValue > 0) && factor != 0f)
                    {
                        actionProbability /= factor;
                    }
                }

                // Add emotional priorities (presence of emotions that are likely to influence the selection of a particular type of Action)
                actionProbability += CalculateEmotionalPriorities(action);

                // Future work: expectations, likelihood to select, personality priorities

                // Add the Action and its grade to the Dictionary
                GradedActions.Add(action, actionProbability);
            }
            Action lowestGradedAction = SelectByValuePercentage(GradedActions);
            targetAgents.ForEach(a => lowestGradedAction.AddAffectedAgent(a));
            return lowestGradedAction;
        }

        private Action SelectByValuePercentage(Dictionary<Action, float> actionValues)
        {
            if (actionValues.Count == 1) return actionValues.First().Key;
            var total = actionValues.Sum(a => a.Value);
            var list = new List<ProportionValue<Action>>();
            foreach (var actionValue in actionValues)
            {
                var val = ((total - actionValue.Value) / total / (actionValues.Count - 1));
                list.Add(ProportionValue.Create(val, actionValue.Key));

            }
            return list.ChooseByRandom();
        }

        private float CalculateEmotionalPriorities(Action action)
        {
            var result = 0f;
            EmotionType prevalentEmotionType = EmotionType.None;
            var max = 0f;
            foreach (var emotion in EmotionalState)
            {
                if (emotion.PositivePole.Second > max)
                {
                    prevalentEmotionType = emotion.Type;
                    max = emotion.PositivePole.Second;
                }
                if (emotion.NegativePole.Second > max)
                {
                    prevalentEmotionType = emotion.Type;
                    max = emotion.NegativePole.Second;
                }
            }
            if (!action.EmotionalPriorities.ContainsKey(prevalentEmotionType))
            {
                // ARBITRARY
                result = Constants.DefaultEmotionalPriorityFactor;
            }
            return result;
        }

        // Modify the agent's memory for target agent with agentId
        private void ModifyMemory(int agentId, float value, bool isReaction = false)
        {
            var finalValue = isReaction ? value * Constants.ReactionReputationModifier : value;
            if (AgentsMemory.ContainsKey(agentId))
            {
                AgentsMemory[agentId] += finalValue;
            }
            else AgentsMemory[agentId] = finalValue;
        }

        // Assess an external event
        private float AssessActionByEvent(Event _event)
        {
            float result = 0f;
            foreach (var inf in _event.EmotionalInfluence)
            {
                var emotion =
                    EmotionalState.FirstOrDefault(
                        e => e.PositivePole.First == inf.Key || e.NegativePole.First == inf.Key);
                //result = emotion.PositivePole.Second > emotion.NegativePole.Second
                result = inf.Value > 0
                ? result + emotion.PositivePole.Second
                : result - emotion.NegativePole.Second;
            }
            return result;
        }

        // Check if the agent should react to the event. Criteria: Threat, emotional modification, expected results
        private bool ShouldReact(Event _event, float emotionalInfluence)
        {
            if (Math.Abs(emotionalInfluence) > Constants.EmotionalInfluenceThreshhold)
                return true;
            return false;
        }

        // Modifies the perceiver agent's EmotionalState
        // See: Computational Models of Emotion and Cognition (paper-3-2-39.pdf) Table 1
        private float PerformEmotionalAppraisal(Event _event)
        {
            var emotionalInfluence = 0f;
            foreach (var emotion in EmotionalState)
            {
                emotionalInfluence += emotion.AffectEmotion(_event, Personality);
            }
            return emotionalInfluence;
        }

        // Called every fixed interval to normalize emotional intensity of the Agent
        public void NormalizeEmotions(float normalizationStep)
        {
            foreach (var emotion in EmotionalState)
            {
                emotion.Normalize(normalizationStep);
            }
        }

        // Add Action to ActionsMemory
        private void AddToActionsMemory(Action action, Reaction reaction)
        {
            var assessment = AssessOwnAction(action, reaction);
            if (ActionsMemory.ContainsKey(action.Id))
            {
                var emotion = ActionsMemory[action.Id].FirstOrDefault(e => e == assessment.First); // += assessment;
                if (emotion != null) emotion.ModifyEmotionValue(assessment.Second);
            }
            else
            {
                ActionsMemory.Add(action.Id, new List<Emotion>() { DetermineEmotionForOwnAction(action) });
            }
        }

        // Future work: Evaluate a resolved Event caused by the Agent 
        private Tuple<Emotion, float> AssessOwnAction(Action action, Reaction reaction)
        {
            // Take into consideration the AffectedAgent and his/her Reaction if any
            var affectedAgentThatReacted = action.AffectedAgents.Where(a => a.Id == reaction.PerformedBy.Id);
            // Return default values (for now)
            return new Tuple<Emotion, float>(EmotionalState.First(), 0f); // Neutral
        }

        // Return the prevalent Emotion towards agent's own action if that action has never been taken before. 
        // Future work: Redefine this process.
        private Emotion DetermineEmotionForOwnAction(Action action)
        {
            var prevalentActionOutcomeEmotion = action.OutcomeEvent.EmotionalInfluence.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            return EmotionalState.First(e => e.PositivePole.First == prevalentActionOutcomeEmotion || e.NegativePole.First == prevalentActionOutcomeEmotion);
        }

        // Fills the ActionsList
        public void SetAvailableActions(List<Action> actionsList)
        {
            AvailableActions = actionsList;
        }
    }

    #endregion

    #region Personality
    [Serializable]
    public class Personality
    {
        [Range(0.1f, 1)]
        public float Agreeableness;
        [Range(0.1f, 1)]
        public float Conscientiousness;
        [Range(0.1f, 1)]
        public float Extraversion;
        [Range(0.1f, 1)]
        public float Neuroticism;
        [Range(0.1f, 1)]
        public float Openness;

        //public Dictionary<PersonalityType, float> PersonalityTypes;

        public Personality(Dictionary<PersonalityType, float> personalityTypes)
        {
            Agreeableness = personalityTypes[PersonalityType.Agreeableness];
            Conscientiousness = personalityTypes[PersonalityType.Conscientiousness];
            Extraversion = personalityTypes[PersonalityType.Extraversion];
            Neuroticism = personalityTypes[PersonalityType.Neuroticism];
            Openness = personalityTypes[PersonalityType.Openness];
        }

        public Personality()
        {

        }
    }
    #endregion
    #region RationalState
    [Serializable]
    public class RationalState
    {
        // TODO: determine the structure and effect of the agent's RationalState
        // [Objective / environment knowledge]
        // Resources
        // EnvironmentFactors
        // MentalState(Cautious, Curious, Excited, Bored) -> Modeling Expectations for Affective Agents paper

        // Environment
        public EnvironmentClassification EnvironmentClassification;

        public void ClassifyEnvironment(EnvironmentClassification classification)
        {
            EnvironmentClassification = classification;
        }

        Event GetExpectedActionOutcome(Action action)
        {
            return new Event();
        }

        internal Tuple<float, float> GetEnvironmentClassificationGrades()
        {
            var positiveRationalFactor = 0f;
            var negativeRationalFactor = 0f;
            switch (EnvironmentClassification)
            {
                case EnvironmentClassification.Friendly:
                    positiveRationalFactor = 1f;
                    negativeRationalFactor = 0.5f;
                    break;
                case EnvironmentClassification.Positive:
                    positiveRationalFactor = 0.9f;
                    negativeRationalFactor = 0.7f;
                    break;
                case EnvironmentClassification.Neutral:
                    positiveRationalFactor = 0.8f;
                    negativeRationalFactor = 0.8f;
                    break;
                case EnvironmentClassification.Negative:
                    positiveRationalFactor = 0.7f;
                    negativeRationalFactor = 0.9f;
                    break;
                case EnvironmentClassification.Hostile:
                    positiveRationalFactor = 0.5f;
                    negativeRationalFactor = 1f;
                    break;
            }
            return new Tuple<float, float>(positiveRationalFactor, negativeRationalFactor);
        }
    }
    #endregion
    #region Emotion
    [Serializable]
    public class Emotion
    {
        public string Name;
        public EmotionType Type;
        public bool FocusSelf;

        public Tuple<EmotionBipole, float> PositivePole;
        public Tuple<EmotionBipole, float> NegativePole;

        public Emotion(string name, EmotionType emotionType, bool focusSelf, Tuple<EmotionBipole, float> positivePole, Tuple<EmotionBipole, float> negativePole)
        {
            Name = name;
            Type = emotionType;
            FocusSelf = focusSelf;
            PositivePole = positivePole;
            NegativePole = negativePole;
        }

        public float AffectEmotion(Event _event, Personality personality)
        {
            var emotion = _event.EmotionalInfluence.FirstOrDefault(i => i.Key == PositivePole.First);
            if (emotion.Key == 0) return 0;
            var personalityFactor = ApplyPersonality(emotion.Key, personality);
            return ModifyEmotionValue(personalityFactor * emotion.Value);
        }

        public void Normalize(float normalizationStep)
        {
            if (PositivePole.Second < 0.5 || PositivePole.Second > 0.5) PositivePole.Second -= normalizationStep;
            NegativePole.Second = 1 - PositivePole.Second;
        }

        // Apply personality tendencies over a specific emotion as a factor
        public float ApplyPersonality(EmotionBipole emotion, Personality personality)
        {
            // Modifiers
            var opennessModifier = 0;
            var conscientiousnessModifier = 0;
            var extraversionModifier = 0;
            var agreeablenessModifier = 0;
            var neuroticismModifier = 0;

            // Personality's OCEAN values
            #region Bipole modifiers
            switch (emotion)
            {
                case EmotionBipole.HappinessFor:
                    opennessModifier = 3;
                    conscientiousnessModifier = 1;
                    extraversionModifier = 5;
                    agreeablenessModifier = 4;
                    neuroticismModifier = 1;
                    break;

                case EmotionBipole.Gloating:
                    opennessModifier = 4;
                    conscientiousnessModifier = 1;
                    extraversionModifier = 5;
                    agreeablenessModifier = 4;
                    neuroticismModifier = 2;
                    break;

                case EmotionBipole.Hope:
                    opennessModifier = 1;
                    conscientiousnessModifier = 4;
                    extraversionModifier = 3;
                    agreeablenessModifier = 1;
                    neuroticismModifier = 5;
                    break;

                case EmotionBipole.Joy:
                    opennessModifier = 4;
                    conscientiousnessModifier = 1;
                    extraversionModifier = 3;
                    agreeablenessModifier = 4;
                    neuroticismModifier = 5;
                    break;

                case EmotionBipole.Pride:
                    opennessModifier = 3;
                    conscientiousnessModifier = 1;
                    extraversionModifier = 4;
                    agreeablenessModifier = 2;
                    neuroticismModifier = 5;
                    break;

                case EmotionBipole.Admiration:
                    opennessModifier = 1;
                    conscientiousnessModifier = 1;
                    extraversionModifier = 4;
                    agreeablenessModifier = 5;
                    neuroticismModifier = 2;
                    break;

                case EmotionBipole.Love:
                    opennessModifier = 1;
                    conscientiousnessModifier = 5;
                    extraversionModifier = 4;
                    agreeablenessModifier = 5;
                    neuroticismModifier = 5;
                    break;

                case EmotionBipole.Satisfaction:
                    opennessModifier = 1;
                    conscientiousnessModifier = 3;
                    extraversionModifier = 3;
                    agreeablenessModifier = 4;
                    neuroticismModifier = 5;
                    break;

                case EmotionBipole.Relief:
                    opennessModifier = 1;
                    conscientiousnessModifier = 1;
                    extraversionModifier = 5;
                    agreeablenessModifier = 2;
                    neuroticismModifier = 5;
                    break;

                case EmotionBipole.Gratification:
                    opennessModifier = 5;
                    conscientiousnessModifier = 5;
                    extraversionModifier = 3;
                    agreeablenessModifier = 2;
                    neuroticismModifier = 5;
                    break;

                case EmotionBipole.Gratitude:
                    opennessModifier = 1;
                    conscientiousnessModifier = 5;
                    extraversionModifier = 2;
                    agreeablenessModifier = 4;
                    neuroticismModifier = 5;
                    break;

                default:
                    break;
            }
            #endregion
            var maxValue =
                new List<float>()
                {
                    opennessModifier * personality.Openness,
                    conscientiousnessModifier * personality.Conscientiousness,
                    extraversionModifier * personality.Extraversion,
                    agreeablenessModifier * personality.Agreeableness,
                    neuroticismModifier * personality.Neuroticism
                }.OrderByDescending(p => p).First();
            //if (maxValue < 0) maxValue = 0.001f;
            return 1 + 1 / (maxValue);
        }

        public float ModifyEmotionValue(float factor)
        {
            var influence = PositivePole.Second / 10 * factor;
            var newVal = PositivePole.Second + influence;
            if (newVal > 0.95f)
            {
                PositivePole.Second = 0.95f;
                NegativePole.Second = 0.05f;
            }
            else if (newVal < 0.05f)
            {
                PositivePole.Second = 0.05f;
                NegativePole.Second = 0.95f;
            }
            else
            {
                PositivePole.Second = newVal;
                NegativePole.Second = 1 - newVal;
            }
            return influence;
        }

    }
    #endregion

    #region Actions & Events
    [Serializable]
    public class Action
    {
        public int Id;
        public string Name;
        public ActionType ActionType;
        public float IntensityValue;
        [NonSerialized]
        public Agent PerformedBy;
        [NonSerialized]
        public List<Agent> AffectedAgents = new List<Agent>();
        public Event OutcomeEvent;
        public List<string> AvailableReactions = new List<string>();
        public List<string> UnavailableReactions = new List<string>();
        // ActionPriority: The lower the number, the higher the chance to occur [0f-1f]
        private float _actionPriority;
        public float ActionPriority
        {
            get { return _actionPriority; }
            set
            {
                if (_actionPriority < 0f) _actionPriority = 0f;
                if (_actionPriority > 1f) _actionPriority = 1f;
            }
        }
        public Dictionary<PersonalityType, float> PersonalityPriorities;
        public Dictionary<EmotionType, float> EmotionalPriorities;
        public Dictionary<EmotionalNeedType, float> EmotionalExpectations;
        public float LikelihoodToSelect = .5f;

        public Action(int id, string name, ActionType actionType, float intensityValue, Event outcomeEvent, float actionPriority, Dictionary<EmotionType, float> emotionalPriorities)
        {
            Id = id;
            Name = name;
            ActionType = actionType;
            IntensityValue = intensityValue;
            //PerformedBy = performedBy;
            //AffectedAgents = affectedAgents;
            OutcomeEvent = outcomeEvent;
            ActionPriority = actionPriority;
            //PersonalityPriorities = personalityPriorities;
            EmotionalPriorities = emotionalPriorities;
            //EmotionalExpectations = emotionalExpectations;
        }

        public Action(Action action)
        {
            Name = action.Name;
            IntensityValue = action.IntensityValue;
            PerformedBy = action.PerformedBy;
            AffectedAgents = action.AffectedAgents;
            OutcomeEvent = action.OutcomeEvent;
            ActionPriority = action.ActionPriority;
            PersonalityPriorities = action.PersonalityPriorities;
            EmotionalPriorities = action.EmotionalPriorities;
            EmotionalExpectations = action.EmotionalExpectations;
        }

        public Action()
        {

        }

        public void AddPerformer(Agent performer)
        {
            PerformedBy = performer;
        }

        public void AddAffectedAgent(Agent agent)
        {
            AffectedAgents.Add(agent);
        }

        // Future work: Integrate expectations, priorities etc
        // Create expectations for current action
        private void CreateExpectations()
        {
            EmotionalExpectations = new Dictionary<EmotionalNeedType, float>();
        }

        private void SetPersonalityPriorities()
        {
            PersonalityPriorities = new Dictionary<PersonalityType, float>();
        }

        private void SetEmotionalPriorities()
        {
            EmotionalPriorities = new Dictionary<EmotionType, float>();
        }
    }

    [Serializable]
    public class Reaction : Action
    {
        int ReactionTo;

        public Reaction(int id, string name, ActionType actionType, float intensityValue, Agent performedBy, List<Agent> affectedAgents, Event outcomeEvent, float actionPriority, Dictionary<PersonalityType, float> personalityPriorities, Dictionary<EmotionType, float> emotionalPriorities, Dictionary<EmotionalNeedType, float> emotionalExpectations, int reactionTo)
            : base(id, name, actionType, intensityValue, outcomeEvent, actionPriority, emotionalPriorities)
        {
            ReactionTo = reactionTo;
        }

        public Reaction(Action action, int reactionTo) : base(action)
        {
            ReactionTo = reactionTo;
        }
    }

    [Serializable]
    public class Event
    {
        public int Id;
        public string Name;
        public Dictionary<EmotionBipole, float> EmotionalInfluence;
    }
    #endregion
}
