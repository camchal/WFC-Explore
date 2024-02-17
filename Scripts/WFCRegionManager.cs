using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using hamsterbyte.WFC;
using System;
using System.Linq;


public class RegionManager {
	private List<WFCRegion> regions;
	private Offset regionOffset;
	private Offset regionOffsetConst;
	private int regionCounter;
	
	

	public RegionManager(int regionWidth, int regionHeight) {
		//inital offset should be zero so that the first region doesnt move
		regionOffset.X = 0; regionOffset.Y = 0;
		//following regions will be offset by the designated size of regions at beggining(all regions will be same size)
		regionOffsetConst.X = regionWidth; regionOffsetConst.Y = regionHeight;
		regions = new List<WFCRegion>();
		regionCounter = 0;
	}
	public void updateRegionOffset(Offset regionOffsetConst){
		regionOffset.X += regionOffsetConst.X;
		//regionOffset.Y += regionOffsetConst.Y;
		regionOffset.Y += 0; //keeping it one row for now

	}

	public void AddRegion(int width, int height, List<WFCRule> rules, bool suppressNotifications = false) {
		regions.Add(new WFCRegion(width, height, rules, regionOffset));
		regionCounter++;
		updateRegionOffset(regionOffset);
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
	
   
   public void CollapseRegions(bool wrap)
	{

		foreach (WFCRegion region in regions)
		{
			if(regionCounter > 1){
				GD.Print("First region succeeded and now I'm starting the second!");

			}
			region.Collapse(wrap);
		}
	}
}

