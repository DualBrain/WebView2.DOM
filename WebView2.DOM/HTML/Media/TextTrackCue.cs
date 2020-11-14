﻿using System;

namespace WebView2.DOM
{
	public class TextTrackCue : EventTarget
	{
		public TextTrack? track => Get<TextTrack?>();

		public string id { get => Get<string>(); set => Set(value); }
		public TimeSpan startTime { get => TimeSpan.FromSeconds(Get<double>()); set => Set(value.TotalSeconds); }
		public TimeSpan endTime { get => TimeSpan.FromSeconds(Get<double>()); set => Set(value.TotalSeconds); }
		public bool pauseOnExit { get => Get<bool>(); set => Set(value); }

		public event EventHandler<Event>? onenter { add => AddEvent(value); remove => RemoveEvent(value); }
		public event EventHandler<Event>? onexit { add => AddEvent(value); remove => RemoveEvent(value); }
	}
}