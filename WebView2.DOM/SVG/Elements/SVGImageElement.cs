﻿using Microsoft.Web.WebView2.Core;
using System.Threading.Tasks;

namespace WebView2.DOM
{
	// https://github.com/chromium/chromium/blob/master/third_party/blink/renderer/core/svg/svg_image_element.idl

	public partial class SVGImageElement : SVGGraphicsElement
	{
		protected internal SVGImageElement(CoreWebView2 coreWebView, string referenceId)
			: base(coreWebView, referenceId) { }

		public SVGAnimatedLength x => Get<SVGAnimatedLength>();
		public SVGAnimatedLength y => Get<SVGAnimatedLength>();
		public SVGAnimatedLength width => Get<SVGAnimatedLength>();
		public SVGAnimatedLength height => Get<SVGAnimatedLength>();
		public SVGAnimatedPreserveAspectRatio preserveAspectRatio => Get<SVGAnimatedPreserveAspectRatio>();

		public ImageDecodingHint decoding { get => Get<ImageDecodingHint>(); set => Set(value); }
		public Task decode() => Get<Task>();
	}

	public partial class SVGImageElement : SVGURIReference
	{
		public SVGAnimatedString href => Get<SVGAnimatedString>();
	}
}