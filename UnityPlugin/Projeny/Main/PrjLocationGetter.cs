using System;
using System.Diagnostics;
using UnityEngine;

namespace Projeny
{
	public class PrjLocationGetter
	{
		public static string GetPrjPath ()
		{
			Process p = new Process ();
			p.StartInfo.FileName = "which";
			p.StartInfo.Arguments = "prj";
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.RedirectStandardError = true;
			p.Start ();

			string output = p.StandardOutput.ReadToEnd ();
			string error = p.StandardError.ReadToEnd ();
			p.WaitForExit ();

			UnityEngine.Debug.Log ("output: " + output);
			UnityEngine.Debug.Log ("error: " + error);

			return output;
		}
	}
}

