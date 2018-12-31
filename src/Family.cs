﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SPEngine
{
	public class TechLevel
	{
		public string techRequired;
		public float entryCost = 0f;
		public float maxThrust;
		public FloatCurve isp;
		public int maxIgnitions;
		public float mass;
		public float cost;
		public float toolCost;
		public float burnTime;

		public TechLevel(ConfigNode node)
		{
			techRequired = node.GetValue("techRequired");
			if (node.HasValue("entryCost"))
				entryCost = float.Parse(node.GetValue("entryCost"));
			maxThrust = float.Parse(node.GetValue("maxThrust"));
			isp = new FloatCurve();
			isp.Load(node.GetNode("isp"));
			maxIgnitions = int.Parse(node.GetValue("maxIgnitions"));
			mass = float.Parse(node.GetValue("mass"));
			cost = float.Parse(node.GetValue("cost"));
			toolCost = float.Parse(node.GetValue("toolCost"));
			burnTime = float.Parse(node.GetValue("burnTime"));
			/* TODO rest of reliability numbers */
		}

		public float getMass(float thrust)
		{
			return (float)Math.Pow(thrust / maxThrust, 0.8f) * mass;
		}
		private float costFactor(float thrust, int ignitions)
		{
			return (float)Math.Pow(thrust / maxThrust, 1.2f) * (float)Math.Pow((ignitions + 1.0f) / (maxIgnitions + 1.0f), 0.2);
		}
		public float getCost(float thrust, int ignitions)
		{
			return costFactor(thrust, ignitions) * cost;
		}
		public float getToolCost(float thrust, int ignitions)
		{
			return costFactor(thrust, ignitions) * toolCost;
		}
		public float getScaleFactor(float thrust)
		{
			return (float)Math.Sqrt(thrust / maxThrust);
		}
	}

	public class Family
	{
		public char letter = '?';
		public string description;
		public Dictionary<string, float> propellants = new Dictionary<string, float>();
		public float minTf = 0.2f;
		public List<TechLevel> techLevels = new List<TechLevel>();
		public int unlocked = 0;
		public Design baseDesign;

		public Family(ConfigNode node)
		{
			letter = node.GetValue("letter")[0];
			description = node.GetValue("description");
			ConfigNode pn = node.GetNode("Propellants");
			foreach (ConfigNode.Value v in pn.values)
				propellants.Add(v.name, float.Parse(v.value));
			if (node.HasValue("minTf"))
				minTf = float.Parse(node.GetValue("minTf"));
			foreach (ConfigNode tn in node.GetNodes("TechLevel"))
				techLevels.Add(new TechLevel(tn));
			baseDesign = new Design(this, 0);
		}

		public void Unlock(int tl)
		{
			if (!check(tl))
				return;
			while (unlocked <= tl) {
				if (!haveTechRequired(unlocked))
					return;
				TechLevel next = techLevels[unlocked];
				if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER) {
					if (Funding.Instance.Funds < next.entryCost)
						return;
					Funding.Instance.AddFunds(-next.entryCost, TransactionReasons.RnDPartPurchase);
				}
				unlocked += 1;
			}
		}

		public float unlockCost(int tl)
		{
			int t = unlocked;
			float result = 0f;
			if (!check(tl))
				return float.NaN;
			while (t <= tl) {
				TechLevel next = techLevels[t];
				result += next.entryCost;
				t += 1;
			}
			return result;
		}

		public bool check(int tl)
		{
			return tl >= 0 && tl < techLevels.Count;
		}

		public float getMaxThrust(int tl)
		{
			if (!check(tl))
				return float.NaN;
			return techLevels[tl].maxThrust;
		}
		public float getMinThrust(int tl)
		{
			return getMaxThrust(tl) * minTf;
		}
		public int getMaxIgnitions(int tl)
		{
			if (!check(tl))
				return 0;
			return techLevels[tl].maxIgnitions;
		}
		public float getMass(int tl, float thrust)
		{
			if (!check(tl))
				return float.NaN;
			return techLevels[tl].getMass(thrust);
		}
		public float getMaxMass(int tl)
		{
			if (!check(tl))
				return float.NaN;
			return techLevels[tl].mass;
		}
		public float getCost(int tl, float thrust, int ignitions)
		{
			if (!check(tl))
				return float.NaN;
			return techLevels[tl].getCost(thrust, ignitions);
		}
		public float getMaxCost(int tl)
		{
			if (!check(tl))
				return float.NaN;
			return techLevels[tl].cost;
		}
		public float getToolCost(int tl, float thrust, int ignitions)
		{
			if (!check(tl))
				return float.NaN;
			return techLevels[tl].getToolCost(thrust, ignitions);
		}
		public FloatCurve getIsp(int tl)
		{
			if (!check(tl))
				return null;
			return techLevels[tl].isp;
		}
		public float getIspAtmo(int tl)
		{
			return getIsp(tl).Evaluate(1.0f);
		}
		public float getIspVac (int tl)
		{
			return getIsp(tl).Evaluate(0.0f);
		}
		public float getBurnTime(int tl)
		{
			if (!check(tl))
				return float.NaN;
			return techLevels[tl].burnTime;
		}
		public string getTechRequired(int tl)
		{
			if (!check(tl))
				return null;
			return techLevels[tl].techRequired;
		}
		public bool haveTechRequired(int tl)
		{
			string tech = getTechRequired(tl);
			if (tech == null || tech.Equals(""))
				return true;
			if (HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX)
				return true;
			return ResearchAndDevelopment.GetTechnologyState(tech) == RDTech.State.Available;
		}
		public float getScaleFactor(int tl, float thrust)
		{
			if (!check(tl))
				return float.NaN;
			return techLevels[tl].getScaleFactor(thrust);
		}
	}
}
