﻿using Microsoft.Web.WebView2.Core;

namespace WebView2.DOM
{
	// https://github.com/chromium/chromium/blob/master/third_party/blink/renderer/core/css/cssom/css_transform_component.idl

	public class CSSTransformComponent : JsObject
	{
		protected internal CSSTransformComponent(CoreWebView2 coreWebView, string referenceId) : base(coreWebView, referenceId)
		{
		}

		public override string ToString() => Method<string>("toString").Invoke();
		public bool is2D { get => Get<bool>(); set => Set(value); }
		public DOMMatrix toMatrix() => Method<DOMMatrix>().Invoke();
	}
}
