﻿using Microsoft.Web.WebView2.Core;
using System;

namespace WebView2.DOM
{
	// https://github.com/chromium/chromium/blob/master/third_party/blink/renderer/core/html/media/html_video_element.idl

	public class HTMLVideoElement : HTMLMediaElement
	{
		protected internal HTMLVideoElement(CoreWebView2 coreWebView, string referenceId) : base(coreWebView, referenceId)
		{
		}

		public uint width { get => Get<uint>(); set => Set(value); }
		public uint height { get => Get<uint>(); set => Set(value); }
		public uint videoWidth => Get<uint>();
		public uint videoHeight => Get<uint>();
		public Uri poster { get => Get<Uri>(); set => Set(value); }

		// The number of frames that have been decoded and made available for
		// playback.
		//readonly attribute unsigned long webkitDecodedFrameCount;

		// The number of decoded frames that have been dropped by the player
		// for performance reasons during playback.
		//readonly attribute unsigned long webkitDroppedFrameCount;

		public bool playsInline { get => Get<bool>(); set => Set(value); }
	}
}
