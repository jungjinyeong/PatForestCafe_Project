
using System.Collections.Generic;
using UnityEngine;

namespace Framework.State
{
    public class StateMachine
    {
        public Dictionary<string, IState> states;

        protected IState activatedState { private set; get; }
        public IState GetAtivated() { return activatedState; }
        public void Initialize()
        {
            states = new Dictionary<string, IState>();
        }
        public void OnUpdate()
        {
            if (activatedState == null)
            {
                //idle 상태만 true해놧음
                List<string> keys = new List<string>(states.Keys);
                for (int i = 0, ii = states.Count; i < ii; i++)
                {
                    if (states[keys[i]].IsTransition())
                    {
                        activatedState = states[keys[i]];
                        activatedState.OperateEnter();
                        break;
                    }
                }
            }
            else if (activatedState != null)
            {
                activatedState.OperateOnUpdate();

                //if (activatedState.IsStateEnd == true)
                //{
                //    activatedState.IsStateEnd = false;
                //    activatedState = null;
                //}

                if(activatedState.IsStateExecute() == false)
                {
                    activatedState = null;
                }
            }
        }
        public void Terminate()
        {
            states.Clear();
            states = null;
        }
        public void Change(string nextState)
        {
            if (activatedState != null)
            {
                activatedState.OperateExit();
            }
            activatedState = states[nextState];
            if (nextState != null)
            {
                activatedState = states[nextState];
                activatedState.OperateEnter();
            }
        }
        public void Add(IState state)
        {
            states.Add(state.Key, state);
        }

        public void Remove(string key)
        {
            states.Remove(key);
        }

        public IState Find(string key)
        {
            if(states.ContainsKey(key))
            {
                return states[key];
            }
            return null;
        }
    }

}

