 namespace hamsterbyte.WFC{
	using System;
	using System.Diagnostics;
	using System.Globalization;
	using System.Net.Sockets;
	using System.Reflection.Metadata;
	using System.Runtime.Serialization;
	using Godot;
	using System.Collections.Generic;
    using System.Linq;

    public partial class WFCGrid{

		public int numattempt = 0;
		public int pushes;
		public int pops;
		public string pushPopHistory;
		private EntropyCoordinates Observe(){
			
			while(!entropyHeap.IsEmpty){
				EntropyCoordinates coords = entropyHeap.Pop();
				pops++;
				pushPopHistory += "o";
				if(!cells[coords.Coordinates.X, coords.Coordinates.Y].Collapsed){ 
					return coords;}
			}
			GD.Print("Heap was emptied!");
			return new EntropyCoordinates { Entropy = -1, Coordinates = new Coordinates { X = -1, Y = -1 } };
		}
		
		private void Collapse(Coordinates _coords){
			int collapsedIndex = cells[_coords.X, _coords.Y].Collapse();//returns collapsed index
			AnimationCoordinates.Enqueue(_coords);
			removalUpdates.Push(new RemovalUpdate(){
				Coordinates = _coords,
				TileIndex = collapsedIndex
			});
			remainingUncollapsedCells--;
		}
		
		private void Propagate(bool _wrap = true){
			if(removalUpdates.Count == 0){
				//stop here bug?
			}
			while(removalUpdates.Count > 0){
				RemovalUpdate update = removalUpdates.Pop();
				if (update.TileIndex == -1){
					validCollapse = false;//generation failed
					return;
				}

				Coordinates[] cardinals = Coordinates.Cardinals;
				for(int d = 0; d < adjacencyRules.GetLength(1); d++){
					Coordinates current = cardinals[d] + update.Coordinates;
					if (_wrap){
						current = current.Wrap(Width, Height);
					} else if (!IsInBounds(current)){
						continue;
					}

					WFCCell currentCell = cells[current.X, current.Y];
					if(currentCell.Collapsed) continue;
					for(int o = 0; o < adjacencyRules.GetLength(2); o++){
						if(adjacencyRules[update.TileIndex, d, o] == 0 && currentCell.Options[o]){
							currentCell.RemoveOption(o);
						}
					}
					
					if(entropyHeap.isHeapFull()){
						entropyHeap = new EntropyHeap(Width * Height);
					}
					
					entropyHeap.Push(new EntropyCoordinates(){
						Coordinates = currentCell.Coordinates,
						Entropy = currentCell.Entropy
					});
					pushes++;
					pushPopHistory += "U";
					//GD.Print($"region {parentRegion.regionIndex.X}, {parentRegion.regionIndex.Y} POST ENTROPYHEAP PUSH");
				}
			}
		}
		
		
		public WFCCell[,] getCellCoordinates(){
			return cells;
		}
		public void TryCollapse(bool _wrap = true, int _maxAttempts = 100){
				Reset(true);
				
				Busy = true;
				Stopwatch timer = Stopwatch.StartNew();
				for(int i  = 0; i < _maxAttempts; i++){
					pushes = 0;
					pops = 0;
					pushPopHistory = " ";
					currentAttempt++;
					GD.Print($"region ({parentRegion.regionIndex.X}, {parentRegion.regionIndex.Y}) is trying a new attempt");
					//GD.Print($"region ({parentRegion.regionIndex.X}, {parentRegion.regionIndex.Y}) heap initliazed to size is {this.width * this.height}");
					//GD.Print($"size of heap is{entropyHeap.getSize()} and num pushes is { pushes} and pops {pops}");
					if(entropyHeap.getSize() != 0){
						GD.Print("entropy heap isnt empty before new attempt ");
					}
					WFCCell cell = cells.Random();
					//WFCCell cell = cells[0,0];

					entropyHeap.Push(new EntropyCoordinates(){
						Coordinates = cell.Coordinates,
						Entropy = cell.Entropy
					});
					pushes++;
					pushPopHistory += "U";

					while (remainingUncollapsedCells > 0){
						EntropyCoordinates e = Observe();
						if (e.Coordinates.X == -1 && e.Coordinates.Y == -1){
							validCollapse = false;
 							break;
						}
						Collapse(e.Coordinates);
						Propagate(_wrap);
					}
					if(!validCollapse && i < _maxAttempts - 1){
						Reset();
					} else{
						if(AnimationCoordinates.Count != 100){
							//missing some tiles
						} 
						break;
					}
				}
				timer.Stop();
				WFCResult result = new(){
					Grid = this,
					Success = validCollapse,
					Attempts = currentAttempt,
					ElapsedMilliseconds = timer.ElapsedMilliseconds
				};
				if(!result.Success){
					ShowResultMetrics(result);
				}
				else{
					GD.Print($"Region ({parentRegion.regionIndex.X},{parentRegion.regionIndex.Y}) suceeded");
					GD.Print($"Attempts: {result.Attempts}");
					GD.Print($"Region ({parentRegion.regionIndex.X},{parentRegion.regionIndex.Y})'s final push count: {pushes}-- pops: {pops}");
					//GD.Print(pushPopHistory);
					GD.Print("------------------------------------");
				}
				//GD.Print($"Success: {result.Success}");
		 		parentRegion.ChildGridCompleted(result);

				Busy = false;
		}
		public void ShowResultMetrics(WFCResult result)
		{
			GD.Print($"Region ({parentRegion.regionIndex.X},{parentRegion.regionIndex.Y} failed max attempts");
			GD.Print($"Grid: {result.Grid}");
			GD.Print($"Success: {result.Success}");
			GD.Print($"Attempts: {result.Attempts}");
			GD.Print($"Elapsed Milliseconds: {result.ElapsedMilliseconds}");
		}
	}
	
}
