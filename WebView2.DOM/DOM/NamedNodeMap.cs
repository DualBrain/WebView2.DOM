﻿using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WebView2.DOM
{
	// https://github.com/chromium/chromium/blob/master/third_party/blink/renderer/core/dom/named_node_map.idl

	[DebuggerTypeProxy(typeof(JsCollectionProxy))]
	public class NamedNodeMap : JsObject, WebView2.DOM.Collections.IReadOnlyCollection<Attr>
	{
		protected internal NamedNodeMap(CoreWebView2 coreWebView, string referenceId)
			: base(coreWebView, referenceId) { }

		//public Attr this[uint index] =>
		//	IndexerGet<Attr?>(index) ?? throw new ArgumentOutOfRangeException(nameof(index));
		public Attr this[string name] =>
			IndexerGet<Attr?>(name) ?? throw new ArgumentException(message: null, paramName: nameof(name));

		public int Count => Get<int>("length");

		public Attr? getNamedItem(string name) => Method<Attr?>().Invoke(name);
		public Attr? getNamedItemNS(string? namespaceURI, string localName) => Method<Attr?>().Invoke(namespaceURI, localName);
		public Attr? setNamedItem(Attr attr) => Method<Attr?>().Invoke(attr);
		public Attr? setNamedItemNS(Attr attr) => Method<Attr?>().Invoke(attr);
		public Attr removeNamedItem(string name) => Method<Attr>().Invoke(name);
		public Attr removeNamedItemNS(string? namespaceURI, string localName) => Method<Attr>().Invoke(namespaceURI, localName);

		public IEnumerator<Attr> GetEnumerator() =>
			SymbolMethod<Iterator<Attr>>("iterator").Invoke();
	}
}
