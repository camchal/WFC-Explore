using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using hamsterbyte.WFC;
using System;
using System.Linq;


public class RegionManager {
	private List<WFCRegion> regions;

	public RegionManager() {
		regions = new List<WFCRegion>();
	}

	public void AddRegion(int width, int height, List<WFCRule> rules, bool suppressNotifications = false) {
		regions.Add(new WFCRegion(width, height, rules));
	}

	public WFCRegion GetRegion(int index) {
		if (index < 0 || index >= regions.Count) {
			throw new IndexOutOfRangeException("Invalid region index");
		}
		return regions[index];
	}

	public bool IsAnyRegionBusy() {
		return regions.Any(region => region.IsBusy());
	}
   
}
