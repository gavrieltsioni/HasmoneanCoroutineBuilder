using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hasmonean.Coroutines
{
    // C# events cannot be passed as data, so we need to invert control
    // and let the invoker subscribe to the event for us.
    // Takes an observed callback (which the invoker must subscribe to their event)
    // and returns an unsubscribe action.
    public delegate Action EventSubscriber(Action observed);

    public class CoroutineBuilder
    {
        protected List<IEnumerator> coroutines = new List<IEnumerator>();

        private bool isRealtime;

        protected CoroutineBuilder(
            bool isRealtime)
        {
            this.isRealtime = isRealtime;
        }

        public static CoroutineBuilder New()
        {
            return new CoroutineBuilder(false);
        }

        public static CoroutineBuilder NewRealtime()
        {
            return new CoroutineBuilder(true);
        }

        public static IEnumerator DoNothing()
        {
            yield break;
        }

    	public static IEnumerator Interpolate<T>(
    		float interpolationRate,
    		Func<float, T> interpolationFunction,
    		Action<T> interpolationAction)
    	{
    		yield return BaseInterpolate<T>(
    			interpolationRate,
    			interpolationFunction,
    			interpolationAction,
    			() => Time.deltaTime);
    	}
    	
    	public static IEnumerator InterpolateRealtime<T>(
    		float interpolationRate,
    		Func<float, T> interpolationFunction,
    		Action<T> interpolationAction)
    	{
    		yield return BaseInterpolate<T>(
    			interpolationRate,
    			interpolationFunction,
    			interpolationAction,
    			() => Time.unscaledDeltaTime);
    	}

    	private static IEnumerator BaseInterpolate<T>(
    		float interpolationRate,
    		Func<float, T> interpolationFunction,
    		Action<T> interpolationAction,
    		Func<float> DeltaTimeSupplier)
    	{
    		for (float interpolation = 0.0f; interpolation < 1.0f; interpolation += interpolationRate * DeltaTimeSupplier())
    		{
    			interpolationAction(interpolationFunction(interpolation));
    			yield return null;
    		}
            // Clamp at 1.0 to ensure final value is included.
            interpolationAction(interpolationFunction(1.0f));
    	}

        public static IEnumerator Timed(
            float time,
            Action Action)
        {
            float timeElapsed = 0.0f;

            while (timeElapsed < time)
            {
                Action();

                yield return null;

                timeElapsed += Time.deltaTime;
            }
        }

        public static IEnumerator Every(
            float interval,
            Action action)
        {
            while (true)
            {
                action();
                yield return new UnityEngine.WaitForSeconds(interval);
            }
        }

        public static IEnumerator Repeat(
            int count,
            Func<IEnumerator> coroutineFactory)
        {
            for (int i = 0; i < count; i++)
            {
                yield return coroutineFactory();
            }
        }

        public static IEnumerator Forever(
            Func<IEnumerator> coroutineFactory)
        {
            while (true)
            {
                yield return coroutineFactory();
            }
        }

        public static IEnumerator Conditional(
            Func<bool> predicate,
            Func<IEnumerator> coroutineFactory)
        {
            if (predicate())
            {
                yield return coroutineFactory();
            }
        }

        public static IEnumerator WaitForSeconds(float seconds)
        {
            yield return new UnityEngine.WaitForSeconds(seconds);
        }

        public static IEnumerator WaitForSecondsRealtime(float seconds)
        {
            yield return new UnityEngine.WaitForSecondsRealtime(seconds);
        }

        public static IEnumerator WaitForAll(
            MonoBehaviour behaviour,
            params IEnumerator[] coroutines)
        {
            List<Coroutine> runningCoroutines = new List<Coroutine>();

            foreach (IEnumerator coroutine in coroutines)
            {
                runningCoroutines.Add(behaviour.StartCoroutine(coroutine));
            }

            foreach (Coroutine runningCoroutine in runningCoroutines)
            {
                yield return runningCoroutine;
            }
        }

        public static IEnumerator WaitUntil(
            Func<bool> predicate)
        {
            yield return new WaitUntil(predicate);
        }

        public static IEnumerator WaitWhile(
            Func<bool> predicate)
        {
            yield return new WaitWhile(predicate);
        }

        public static IEnumerator WaitForEvent(
            EventSubscriber eventSubscriber)
        {
            bool observed = false;

            Action unsubscribe = eventSubscriber(() => observed = true);

            yield return WaitUntil(() => observed);
            unsubscribe();
        }

        public static IEnumerator Action(Action action)
        {
            action();
            yield break;
        }

        public static IEnumerator DelayAction(float delay, Action action)
        {
            yield return CoroutineBuilder.New()
                .ThenWaitForSeconds(delay)
                .ThenDo(action)
                .Build();
        }

        public static IEnumerator DoAtEndOfFrame(Action action)
        {
            yield return new WaitForEndOfFrame();
            action();
        }

        public static IEnumerator DoAtFixedUpdate(Action action)
        {
            yield return new WaitForFixedUpdate();
            action();
        }

        public CoroutineBuilder ThenWait(IEnumerator coroutine)
        {
            coroutines.Add(coroutine);
            return this;
        }

        public CoroutineBuilder ThenWait(Func<IEnumerator> CoroutineSupplier)
        {
            coroutines.Add(CoroutineSupplier());
            return this;
        }

        public CoroutineBuilder ThenWaitForSeconds(float seconds)
        {
            if (isRealtime)
            {
                coroutines.Add(CoroutineBuilder.WaitForSecondsRealtime(seconds));
            }
            else
            {
                coroutines.Add(CoroutineBuilder.WaitForSeconds(seconds));
            }
            return this;
        }

        public CoroutineBuilder ThenWaitForAll(MonoBehaviour behaviour, List<IEnumerator> awaitables)
        {
            coroutines.Add(CoroutineBuilder.WaitForAll(behaviour, awaitables.ToArray()));
            return this;
        }

        public CoroutineBuilder ThenWaitForAll(MonoBehaviour behaviour, params IEnumerator[] awaitables)
        {
            coroutines.Add(CoroutineBuilder.WaitForAll(behaviour, awaitables));
            return this;
        }

        public CoroutineBuilder ThenWaitUntil(Func<bool> Predicate)
        {
            coroutines.Add(CoroutineBuilder.WaitUntil(Predicate));
            return this;
        }

        public CoroutineBuilder ThenWaitWhile(Func<bool> Predicate)
        {
            coroutines.Add(CoroutineBuilder.WaitWhile(Predicate));
            return this;
        }

        public CoroutineBuilder ThenWaitForEvent(EventSubscriber EventSubscriber)
        {
            coroutines.Add(CoroutineBuilder.WaitForEvent(EventSubscriber));
            return this;
        }

        public CoroutineBuilder ThenDo(Action action)
        {
            coroutines.Add(CoroutineBuilder.Action(action));
            return this;
        }

        public CoroutineBuilder ThenDo(List<Action> actions)
        {
            return ThenDo(actions.ToArray());
        }

        public CoroutineBuilder ThenDo(params Action[] actions)
        {
            coroutines.Add(CoroutineBuilder.Action(() =>
            {
                foreach (Action action in actions)
                {
                    action();
                }
            }));
            return this;
        }

        public CoroutineBuilder ThenNotify(Action notifiable)
        {
            coroutines.Add(CoroutineBuilder.Action(notifiable));
            return this;
        }

        public CoroutineBuilder ThenNotify(List<Action> notifiables)
        {
            return ThenNotify(notifiables.ToArray());
        }

        public CoroutineBuilder ThenNotify(params Action[] notifiables)
        {
            coroutines.Add(CoroutineBuilder.Action(() =>
            {
                foreach (Action notifiable in notifiables)
                {
                    notifiable();
                }
            }));
            return this;
        }

        public CoroutineBuilder ThenLog(String message)
        {
            coroutines.Add(CoroutineBuilder.Action(() => Debug.Log(message)));
            return this;
        }

        public CoroutineBuilder ThenStart(MonoBehaviour parent, IEnumerator startable)
        {
            coroutines.Add(CoroutineBuilder.Action(() => parent.StartCoroutine(startable)));
            return this;
        }

        public CoroutineBuilder ThenStart(MonoBehaviour parent, List<IEnumerator> startables)
        {
            return ThenStart(parent, startables.ToArray());
        }

        public CoroutineBuilder ThenStart(MonoBehaviour parent, params IEnumerator[] startables)
        {
            coroutines.Add(CoroutineBuilder.Action(() => 
            {
                foreach (IEnumerator coroutine in startables)
                {
                    parent.StartCoroutine(coroutine);
                }
            }));
            return this;
        }

        public CoroutineBuilder ThenStop(MonoBehaviour parent, IEnumerator stoppable)
        {
            coroutines.Add(CoroutineBuilder.Action(() => parent.StopCoroutine(stoppable)));
            return this;
        }

        public CoroutineBuilder ThenStop(MonoBehaviour parent, List<IEnumerator> stoppables)
        {
            return ThenStop(parent, stoppables.ToArray());
        }

        public CoroutineBuilder ThenStop(MonoBehaviour parent, params IEnumerator[] stoppables)
        {
            coroutines.Add(CoroutineBuilder.Action(() => 
            {
                foreach (IEnumerator coroutine in stoppables)
                {
                    parent.StopCoroutine(coroutine);
                }
            }));
            return this;
        }

        public CoroutineBuilder ThenInterpolate<T>(
            float interpolationRate,
            Func<float, T> interpolationFunction,
            Action<T> interpolationAction)
        {
            if (isRealtime)
            {
                coroutines.Add(CoroutineBuilder.InterpolateRealtime(
                    interpolationRate, interpolationFunction, interpolationAction));
            }
            else
            {
                coroutines.Add(CoroutineBuilder.Interpolate(
                    interpolationRate, interpolationFunction, interpolationAction));
            }
            return this;
        }

        public IEnumerator Build()
        {
            for (int i = 0; i < coroutines.Count; i++)
            {
                yield return coroutines[i];
            }
        }
    }
}