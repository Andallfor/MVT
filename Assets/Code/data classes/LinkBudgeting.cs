using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading.Tasks;

public static class linkBudgeting
{
	public static Dictionary<string, (bool, double, double)> users = new Dictionary<string, (bool, double, double)>();
	public static Dictionary<string, (bool, double, double)> providers = new Dictionary<string, (bool, double, double)>();
}
