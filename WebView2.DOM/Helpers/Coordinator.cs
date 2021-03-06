﻿using Microsoft.Web.WebView2.Core;
using SmartAnalyzers.CSharpExtensions.Annotations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WebView2.DOM
{
	internal record CoordinatorCall
	{
		[InitRequired] public string referenceId { get; init; }
		[InitRequired] public string memberType { get; init; }
		[InitRequired] public string memberName { get; init; }
		public object?[]? parameters { get; init; }
	}

	public sealed class Coordinator
	{
		//private BlockingCollection<string> calls;
		//private readonly BlockingCollection<object?> objects = new BlockingCollection<object?>();
		//private IEnumerator<string> enumerator;

		private readonly ConcurrentDictionary<string, BlockingCollection<object>> callsDict =
			new ConcurrentDictionary<string, BlockingCollection<object>>();

		private readonly ConcurrentDictionary<string, BlockingCollection<object?>> objectsDict =
			new ConcurrentDictionary<string, BlockingCollection<object?>>();

		private readonly ConcurrentDictionary<string, IEnumerator<object>> enumeratorDict =
			new ConcurrentDictionary<string, IEnumerator<object>>();

		private readonly CoreWebView2 coreWebView;
		private CancellationTokenSource cts;
		private readonly AsyncLocal<CancellationTokenSource?> asyncLocalCts = new AsyncLocal<CancellationTokenSource?>();

		private CancellationToken CancellationToken =>
			asyncLocalCts.Value?.Token ?? throw new OperationCanceledException();

		private BlockingCollection<object> Calls(string windowId) =>
			callsDict.GetOrAdd(windowId, _ => throw new OperationCanceledException());

		private BlockingCollection<object?> Objects(string windowId) =>
			objectsDict.GetOrAdd(windowId, _ => throw new OperationCanceledException());

		internal Coordinator(CoreWebView2 coreWebView)
		{
			//calls = new BlockingCollection<string>();
			//enumerator = calls.GetConsumingEnumerable().GetEnumerator();
			cts = new CancellationTokenSource();
			this.coreWebView = coreWebView;
		}

		internal void CancelRunningThreads()
		{
			cts.Cancel();
			callsDict.Clear();
			objectsDict.Clear();
			enumeratorDict.Clear();
		}

		#region Called from JavaScript: Entry points

		public void RaiseEvent(string windowId, string eventTargetId, string eventName, string eventId)
		{
			Reset(windowId);
			Task.Run(() =>
			{
				SynchronizationContext.SetSynchronizationContext(WebViewSynchronizationContext.For(coreWebView));
				try
				{
					asyncLocalCts.Value = cts;
					window.SetInstance(References.Get<Window>(windowId));
					var eventTarget = References.Get<EventTarget>(eventTargetId);
					var eventObject = References.Get<Event>(eventId);
					eventTarget.RaiseEvent(eventName, eventObject);
				}
				finally
				{
					window.SetInstance(null);
					Calls(windowId).CompleteAdding();
				}
			});
		}

		public void FulfillPromise(string windowId, string promiseId, string json, bool isComplete)
		{
			json ??= "null";
			var tcs = References.GetTaskCompletionSource(promiseId);
			tcs.SetResult(json);
		}

		public void RejectPromise(string windowId, string promiseId, string json, bool isComplete)
		{
			json ??= "null";
			var tcs = References.GetTaskCompletionSource(promiseId);
			var errorWrapper = JsonSerializer.Deserialize<ErrorWrapper>(json, coreWebView.Options());
			if (errorWrapper is null)
			{
				tcs.SetException(new NullReferenceException());
				return;
			}
			var ex = errorWrapper.GetException();

			tcs.SetException(ex);
		}

		private readonly ConcurrentDictionary<string, Action<string>> _onRun = new();

		public void SyncContextPost(SendOrPostCallback d, object? state)
		{
			var runId = System.Guid.NewGuid().ToString();

			_onRun.TryAdd(runId, windowId =>
			{
				Task.Run(() =>
				{
					SynchronizationContext.SetSynchronizationContext(WebViewSynchronizationContext.For(coreWebView));
					try
					{
						asyncLocalCts.Value = cts;
						var w = References.Get<Window>(windowId);
						window.SetInstance(w);
						d(state);
					}
					finally
					{
						window.SetInstance(null);
						Calls(windowId).CompleteAdding();
					}
				});
			});

			_ = coreWebView.ExecuteScriptAsync($@"
				(() => {{
					const Coordinator = () => window.chrome.webview.hostObjects.sync.Coordinator;
					Coordinator().{nameof(OnRun)}(WebView2DOM.GetId(window), '{runId}');
					WebView2DOM.EventLoop();
				}})()
			");
		}

		public void OnRun(string windowId, string runId)
		{
			Reset(windowId);
			if (_onRun.TryRemove(runId, out var action))
			{
				action(windowId);
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		public void OnCallback(string windowId, string callbackId, string json)
		{
			json ??= "null";
			if (Calls(windowId).IsCompleted)
			{
				Reset(windowId);
				Task.Run(() =>
				{
					SynchronizationContext.SetSynchronizationContext(WebViewSynchronizationContext.For(coreWebView));
					try
					{
						asyncLocalCts.Value = cts;
						window.SetInstance(References.Get<Window>(windowId));
						func();
					}
					finally
					{
						window.SetInstance(null);
						Calls(windowId).CompleteAdding();
					}
				});
			}
			else
			{
				func();
			}

			void func()
			{
				var callback = References.GetCallback(callbackId);

				var parameters = JsonSerializer.Deserialize<ImmutableList<JsonElement>>(json, coreWebView.Options())
					?? ImmutableList<JsonElement>.Empty;

				var parametersInfo = callback.Method.GetParameters();

				if (parametersInfo.Length != parameters.Count)
				{
					throw new InvalidOperationException("Error invoking callback: number of parameters doesn't match");
				}

				var final = Enumerable.Zip(parameters, parametersInfo,
					(p, i) => JsonSerializer.Deserialize(p.GetRawText(), i.ParameterType, coreWebView.Options()))
					.ToArray<object?>();

				callback.DynamicInvoke(args: final);
			}
		}
		#endregion

		#region Called from JavaScript: IEnumerator
		public string Current(string windowId) => (string)enumeratorDict.GetOrAdd(windowId, _ => throw new InvalidOperationException()).Current;

		public bool MoveNext(string windowId)
		{
			var enumerator = enumeratorDict.GetOrAdd(windowId, _ => throw new InvalidOperationException());
			while (enumerator.MoveNext())
			{
				if (enumerator.Current is string)
				{
					return true;
				}
				else if (enumerator.Current is Action action)
				{
					action();
				}
			}
			return false;
		}

		public void Reset(string windowId)
		{
			var calls = callsDict.AddOrUpdate(windowId,
				_ => new BlockingCollection<object>(),
				(_, __) => new BlockingCollection<object>());
			//calls = new BlockingCollection<string>();
			enumeratorDict.AddOrUpdate(windowId,
				_ => calls.GetConsumingEnumerable().GetEnumerator(),
				(_, __) => calls.GetConsumingEnumerable().GetEnumerator());
			//enumerator = calls.GetConsumingEnumerable().GetEnumerator();

			objectsDict.GetOrAdd(windowId,
				_ => new BlockingCollection<object?>());

			cts = new CancellationTokenSource();
		}
		#endregion

		#region Called from JavaScript: Return to C#
		public void ReturnVoid(string windowId)
		{
			Objects(windowId).Add(null);
		}

		public void ReturnValue(string windowId, string json)
		{
			Objects(windowId).Add(json ?? "null");
		}

		public void Throw(string windowId, string json)
		{
			var errorWrapper = JsonSerializer.Deserialize<ErrorWrapper>(json ?? "null", coreWebView.Options());
			Objects(windowId).Add(errorWrapper?.GetException() ?? (Exception)new NullReferenceException());
		}
		#endregion

		#region Called from C#
		internal void Call(CoordinatorCall call)
		{
			Debugger.NotifyOfCrossThreadDependency();
			CancellationToken.ThrowIfCancellationRequested();
			var windowId = window.Instance.referenceId;
			try
			{
				Calls(windowId).Add(JsonSerializer.Serialize(call, coreWebView.Options()), CancellationToken);
			}
			catch (InvalidOperationException ex) when (ex.Source == "System.Collections.Concurrent")
			{
				throw new InvalidOperationException("The calling thread cannot access this object because a different thread owns it.");
			}

			switch (Objects(windowId).Take(CancellationToken))
			{
			case Exception ex: throw ex;
			case null: break;
			default: throw new InvalidOperationException("should never happen");
			}
		}

		internal T Call<T>(CoordinatorCall call)
		{
			Debugger.NotifyOfCrossThreadDependency();
			CancellationToken.ThrowIfCancellationRequested();
			var windowId = window.Instance.referenceId;
			try
			{
				Calls(windowId).Add(JsonSerializer.Serialize(call, coreWebView.Options()), CancellationToken);
			}
			catch (InvalidOperationException ex) when (ex.Source == "System.Collections.Concurrent")
			{
				throw new InvalidOperationException("The calling thread cannot access this object because a different thread owns it.");
			}

			return (Objects(windowId).Take(CancellationToken)) switch
			{
				Exception ex => throw ex,
				string json => JsonSerializer.Deserialize<T>(json, coreWebView.Options())!,
				_ => throw new InvalidOperationException("should never happen"),
			};
		}

		internal void EnqueueUiThreadAction(Action action)
		{
			Debugger.NotifyOfCrossThreadDependency();
			CancellationToken.ThrowIfCancellationRequested();
			var windowId = window.Instance.referenceId;
			try
			{
				Calls(windowId).Add(action, CancellationToken);
			}
			catch (InvalidOperationException ex) when (ex.Source == "System.Collections.Concurrent")
			{
				throw new InvalidOperationException("The calling thread cannot access this object because a different thread owns it.");
			}
		}
		#endregion
	}
}
