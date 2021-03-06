﻿using UnityEngine;

namespace PiRhoSoft.Utilities
{
	[AddComponentMenu("PiRho Utilities/Button")]
	public class ButtonSample : MonoBehaviour
	{
		[MessageBox("The [Button] attribute adds a button next to the field that calls a method when clicked. The method can be public/private, optionally static, and takes no parameters.", MessageBoxType.Info, Location = TraitLocation.Above)]

		[Button(nameof(PublicStatic), "Public/Static", Location = TraitLocation.Above)]
		public string Above;

		[Button(nameof(PrivateStatic), "Private/Static", Location = TraitLocation.Below)]
		public string Below;

		[Button(nameof(PublicInstance), "Public/Instance", Location = TraitLocation.Right)]
		public string ToTheRight;

		[Button(nameof(PrivateInstance), "Private/Instance", Location = TraitLocation.Left)]
		public string ToTheLeft;

		public static void PublicStatic()
		{
			Debug.Log("Called a public static method");
		}

		private static void PrivateStatic()
		{
			Debug.Log("Called a private static method");
		}

		public void PublicInstance()
		{
			Debug.Log("Called a public instance method", this);
		}

		private void PrivateInstance()
		{
			Debug.Log("Called a private instance method", this);
		}
	}
}