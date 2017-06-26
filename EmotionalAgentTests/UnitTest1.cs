using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EmotionalAgentLib;
using MainLibrary;

namespace EmotionalAgentTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestAgentCreation()
        {
            ActionsLibrary.InitializeTestActions();

            var agent1 = new Agent(100, PersonalityLibrary.DefaultPersonality, RationalLibrary.GetDefaultRationalState(), EmotionsLibrary.GetDefaultEmotionalState(), ActionsLibrary.AvailableActions);
            var agent2 = new Agent(200, PersonalityLibrary.DefaultPersonality, RationalLibrary.GetDefaultRationalState(), EmotionsLibrary.GetDefaultEmotionalState(), ActionsLibrary.AvailableActions);

            // Cranky personality
            agent2.Personality.Agreeableness = 0.1f;
            agent2.Personality.Conscientiousness = 0.1f;
            agent2.Personality.Extraversion = 0.1f;
            agent2.Personality.Neuroticism = 0.1f;
            agent2.Personality.Openness = 0.1f;
            agent2.FreeReactionSelection = true;
            agent2.FreeActionSelection = true;

            agent1.SetEnvironment(EnvironmentClassification.Neutral);
            agent2.SetEnvironment(EnvironmentClassification.Neutral);

            var primaryAction = agent1.Act(new List<Agent>() { agent2 }, agent1.AvailableActions.First(a => a.Name == Constants.ActionAttack));

            if (primaryAction == null)
            {
                throw new ArgumentNullException("Error: no action specified");
            }

            // Appraise and react
            var agent2Reaction = agent2.GetActionFeedBack(primaryAction);

            if (agent2Reaction == null)
            {
                throw new ArgumentNullException("Error: no reaction specified to external agent's action");
            }

            // Attack self
            var agent2Action = agent2.Act(new List<Agent>() { agent2 }, agent2.AvailableActions.First(a => a.Name == Constants.ActionAttack));

            //Appraise and react
            agent2Reaction = agent2.GetActionFeedBack(agent2Action);

            agent1.NormalizeEmotions(.001f);
            agent2.NormalizeEmotions(.001f);

            if (agent2Reaction != null)
            {
                throw new ArgumentNullException("Error: reaction specified to own agent's action");
            }

        }
    }
}