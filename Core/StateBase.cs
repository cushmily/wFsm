﻿using System;
using System.Collections.Generic;

namespace wFSM
{
    public abstract class StateBase : IState
    {
        #region IState Implementation

        public IState Parent { get; set; }

        public virtual void Enter()
        {
            if (OnEnter != null) { OnEnter.Invoke(); }
        }

        public void Update(float deltaTime)
        {
            // Only update the lastest state
            if (_activeChidren.Count > 0)
            {
                _activeChidren.Peek().Update(deltaTime);
                return;
            }

            if (OnUpdate != null) { OnUpdate.Invoke(deltaTime); }

            // Check if condition meets
            foreach (var conditionPair in _conditions)
            {
                if (conditionPair.Key.Invoke() && conditionPair.Value != null) { conditionPair.Value.Invoke(); }
            }
        }

        public void Exit()
        {
            if (OnExit != null) { OnExit.Invoke(); }
        }

        public void ChangeState(string name)
        {
            IState result;
            if (!_children.TryGetValue(name, out result))
            {
                throw new ApplicationException(string.Format("Child state [{0}] not found.", name));
            }

            if (_activeChidren.Count > 0) { PopState(); }

            PushState(result);
        }

        public void PushState(string name)
        {
            IState result;
            if (!_children.TryGetValue(name, out result))
            {
                throw new ApplicationException(string.Format("Child state [{0}] not found.", name));
            }

            PushState(result);
        }

        public void PopState(string name)
        {
            IState result;
            if (!_children.TryGetValue(name, out result))
            {
                throw new ApplicationException(string.Format("Child state [{0}] not found.", name));
            }

            PopState();
        }

        public void TriggerEvent(string id)
        {
            TriggerEvent(id, EventArgs.Empty);
        }

        public void TriggerEvent(string id, EventArgs eventArgs)
        {
            if (_activeChidren.Count > 0)
            {
                _activeChidren.Peek().TriggerEvent(id, eventArgs);
                return;
            }

            Action<EventArgs> action;
            if (!_events.TryGetValue(id, out action))
            {
                throw new ApplicationException(string.Format("Event [{0}] not exits.", id));
            }

            if (action != null) { action.Invoke(eventArgs); }
        }

        #endregion

        #region Actions

        public event Action OnEnter;
        public event Action OnExit;
        public event Action<float> OnUpdate;

        #endregion

        #region Runtime

        private readonly Stack<IState> _activeChidren = new Stack<IState>();
        private readonly Dictionary<string, IState> _children = new Dictionary<string, IState>();
        private readonly Dictionary<string, Action<EventArgs>> _events = new Dictionary<string, Action<EventArgs>>();
        private readonly Dictionary<Func<bool>, Action> _conditions = new Dictionary<Func<bool>, Action>();

        #endregion

        #region Private Operations

        private void PopState()
        {
            var result = _activeChidren.Pop();
            result.Exit();
        }

        private void PushState(IState state)
        {
            _activeChidren.Push(state);
            state.Enter();
        }

        #endregion

        #region Helper

        public void AddChild(string name, IState state)
        {
            if (!_children.ContainsKey(name))
            {
                _children.Add(name, state);
                state.Parent = this;
            }
            else { throw new ApplicationException(string.Format("Child state already exists: {0}", name)); }
        }

        public void SetEnterAction(Action onEnter)
        {
            OnEnter = onEnter;
        }

        public void SetExitAction(Action onExit)
        {
            OnExit = onExit;
        }

        public void SetUpdateAction(Action<float> onUpdate)
        {
            OnUpdate = onUpdate;
        }

        public void SetEvent(string id, Action<EventArgs> action)
        {
            if (!_events.ContainsKey(id)) { _events.Add(id, action); }
            else { throw new ApplicationException(string.Format("Event already exists: {0}", id)); }
        }

        public void SetEvent<TArgs>(string id, Action<TArgs> action) where TArgs : EventArgs
        {
            if (!_events.ContainsKey(id)) { _events.Add(id, arg => { action.Invoke((TArgs) arg); }); }
            else { throw new ApplicationException(string.Format("Event already exists: {0}", id)); }
        }

        public void AddCondition(Func<bool> predicate, Action action)
        {
            _conditions.Add(predicate, action);
        }

        #endregion
    }
}