using UnityEngine;

namespace SinWheel
{
    public static class InterludeGameFactory
    {
        /// <summary>Seven ids, seven verbs. Two of the same verb would collapse into one memory.</summary>
        public static InterludeGame Create(string id)
        {
            switch (id)
            {
                case "ember": return new EmberGame();
                case "mirror": return new MirrorGame();
                case "shell": return new ShellGame();
                case "feast": return new FeastGame();
                case "toll": return new TollGame();
                case "vigil": return new VigilGame();
                case "understudy": return new UnderstudyGame();
                default:
                    Debug.LogError($"[Interlude] No game for id '{id}'");
                    return null;
            }
        }

        /// <summary>Games that read press and release rather than a tap.</summary>
        public static bool UsesHold(string id) => id == "feast" || id == "vigil";

        /// <summary>Games driven by a tap anywhere rather than by tapping a thing.</summary>
        public static bool UsesTapSurface(string id) => id == "ember" || id == "toll";
    }
}
