﻿using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts.Player;
using Assets.Scripts.Timers;
using Assets.Scripts.Util;
using TeamUtility.IO;

namespace Assets.Scripts.Data
{
    public class GameManager : MonoBehaviour
    {
        // Use a singleton instance to make sure there is only one
        public static GameManager instance;

        private List<Controller> controllers;

        [SerializeField]
        private List<Enums.Tokens> enabledTokens;
        [SerializeField]
        private List<GameObject> allTokens;
        private Dictionary<Enums.Tokens, Enums.Frequency> tokens;

        // Sets up singleton instance. Will remain if one does not already exist in scene
        void Awake()
        {
            if (instance == null)
            {
                DontDestroyOnLoad(gameObject);
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }

            controllers = new List<Controller>();
            tokens = new Dictionary<Enums.Tokens, Enums.Frequency>();
            tokens.Add(Enums.Tokens.Fireball, Enums.Frequency.Abundant);
            tokens.Add(Enums.Tokens.Ghost, Enums.Frequency.Abundant);
            tokens.Add(Enums.Tokens.Ricochet, Enums.Frequency.Abundant);
            TokenSpawner.instance.Init(tokens);
        }

        void Start()
        {
            Controller[] findControllers = FindObjectsOfType<Controller>();
            for(int i = 0; i < findControllers.Length; i++)
            {
                controllers.Add(findControllers[i]);
            }
        }

        public void Respawn(PlayerID id)
        {
            Controller deadPlayer = controllers.Find(x => x.ID.Equals(id));
            if(deadPlayer != null)
            {
                CountdownTimer t = gameObject.AddComponent<CountdownTimer>();
                t.Initialize(3f, deadPlayer.ID.ToString());
                t.TimeOut += new CountdownTimer.TimerEvent(ResawnHelper);
            }
        }

        private void ResawnHelper(CountdownTimer t)
        {
            Controller deadPlayer = controllers.Find(x => x.ID.Equals(System.Enum.Parse(typeof(PlayerID), t.ID)));
            if (deadPlayer != null)
            {
                // Find an appropriate spawning pod (set to default for now)
                deadPlayer.transform.position = Vector3.zero;
                // Let the player revive itself
                deadPlayer.LifeComponent.Respawn();
            }
        }

        #region C# Properties
        public List<GameObject> AllTokens
        {
            get { return allTokens; }
        }
        #endregion
    }
}
