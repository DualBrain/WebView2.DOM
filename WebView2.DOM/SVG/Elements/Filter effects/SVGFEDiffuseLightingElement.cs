﻿using Microsoft.Web.WebView2.Core;

namespace WebView2.DOM
{
	// https://github.com/chromium/chromium/blob/master/third_party/blink/renderer/core/svg/svg_fe_diffuse_lighting_element.idl

	public partial class SVGFEDiffuseLightingElement : SVGElement
	{
		protected internal SVGFEDiffuseLightingElement(CoreWebView2 coreWebView, string referenceId)
			: base(coreWebView, referenceId) { }

		public SVGAnimatedString in1 => Get<SVGAnimatedString>();
		public SVGAnimatedNumber surfaceScale => Get<SVGAnimatedNumber>();
		public SVGAnimatedNumber diffuseConstant => Get<SVGAnimatedNumber>();
		public SVGAnimatedNumber kernelUnitLengthX => Get<SVGAnimatedNumber>();
		public SVGAnimatedNumber kernelUnitLengthY => Get<SVGAnimatedNumber>();
	}

	public partial class SVGFEDiffuseLightingElement : SVGFilterPrimitiveStandardAttributes
	{
		public SVGAnimatedLength x => Get<SVGAnimatedLength>();
		public SVGAnimatedLength y => Get<SVGAnimatedLength>();
		public SVGAnimatedLength width => Get<SVGAnimatedLength>();
		public SVGAnimatedLength height => Get<SVGAnimatedLength>();
		public SVGAnimatedString result => Get<SVGAnimatedString>();
	}
}