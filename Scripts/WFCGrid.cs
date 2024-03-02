 namespace hamsterbyte.WFC{
	using System;
	using System.Diagnostics;
	using System.Globalization;
	using System.Net.Sockets;
	using System.Reflection.Metadata;
	using System.Runtime.Serialization;
	using Godot;
	using System.Collections.Generic;

	

	public partial class WFCGrid{

		public int numattempt = 0;
		private EntropyCoordinates Observe(){
			
			while(!entropyHeap.IsEmpty){
				EntropyCoordinates coords = entropyHeap.Pop();
				WFCCell [,] testCells = cells;
				if(!cells[coords.Coordinates.X, coords.Coordinates.Y].Collapsed){ 
					return coords;}
			}
			GD.Print("Heap was emptied!");
			return EntropyCoordinates.Invalid;
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
							//GD.Print($"region {parentRegion.regionIndex.X}, {parentRegion.regionIndex.Y} POST CARDINALS");
							//GD.Print($"EntropyHeap size: {entropyHeap.getSize()}");
							if(entropyHeap.isHeapFull()){
								EntropyHeap dummyHeap = entropyHeap;
								GD.Print($"heap size: {dummyHeap.getSize()}");

								GD.Print($"EntropyHeap full: {entropyHeap.isHeapFull()}");

								HashSet<Vector2> seenCoordinates = new HashSet<Vector2>();
								List<Vector2> repeatedCoordinates = new List<Vector2>();
								int heapSize = dummyHeap.getSize();
								for (int i = 0; i < heapSize; i++) {
									EntropyCoordinates e = dummyHeap.Pop();
									Vector2 currentCoordinates = new Vector2(e.Coordinates.X, e.Coordinates.Y);
									if (seenCoordinates.Contains(currentCoordinates)) {
										repeatedCoordinates.Add(currentCoordinates);
									} else {
										seenCoordinates.Add(currentCoordinates);
									}
									
								}
								foreach (Vector2 repeatedCoordinate in repeatedCoordinates) {
										GD.Print($"Repeated coordinate: ({repeatedCoordinate.X}, {repeatedCoordinate.Y})");
									}
					}
					
					entropyHeap.Push(new EntropyCoordinates(){
						Coordinates = currentCell.Coordinates,
						Entropy = currentCell.Entropy
					});
					//GD.Print($"region {parentRegion.regionIndex.X}, {parentRegion.regionIndex.Y} POST ENTROPYHEAP PUSH");
				}
			}
		}
		
		public void updateCellCoordinates(Offset regionOffset){
			// GD.Print($"Updating Region {parentRegion.regionNumber}'s cells");
			for(int x = 0; x < width; x++){
				for(int y = 0 ; y < height; y++){
				//cells[x, y].UpdateCoordinates(regionOffset.X, regionOffset.Y, parentRegion.regionNumber);
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
					currentAttempt++;
					GD.Print($"region {parentRegion.regionIndex.X}, {parentRegion.regionIndex.Y} is trying a new attempt");
					GD.Print($"heap size is {this.width * this.height}");
					if(entropyHeap.getSize() != 0){
						GD.Print("entropy heap isnt empty before new attempt ");
					}
					WFCCell cell = cells.Random();

					entropyHeap.Push(new EntropyCoordinates(){
						Coordinates = cell.Coordinates,
						Entropy = cell.Entropy
					});
					//GD.Print($"region {parentRegion.regionIndex.X}, {parentRegion.regionIndex.Y} PUSHED NEW COORDS");

					while (remainingUncollapsedCells > 0){
						EntropyCoordinates e = Observe();
						//GD.Print($"region {parentRegion.regionIndex.X}, {parentRegion.regionIndex.Y} POST OBSERVE");
						Collapse(e.Coordinates);
						//GD.Print($"region {parentRegion.regionIndex.X}, {parentRegion.regionIndex.Y} POST COLLAPSE");
						Propagate(_wrap);
						//GD.Print($"region {parentRegion.regionIndex.X}, {parentRegion.regionIndex.Y} POST PROPGATE");
					}
					//GD.Print($"region {parentRegion.regionIndex.X}, {parentRegion.regionIndex.Y} POST WHILE LOOP");
					if(!validCollapse && i < _maxAttempts - 1){
						Reset();
					} else break;
				}
				timer.Stop();
				WFCResult result = new(){
					Grid = this,
					Success = validCollapse,
					Attempts = currentAttempt,
					ElapsedMilliseconds = timer.ElapsedMilliseconds
				};
				//GD.Print($"region {parentRegion.regionIndex.Y} got to the end");
				if(!result.Success){
					ShowResultMetrics(result);
				}
				else{
					GD.Print($"Region ({parentRegion.regionIndex.X},{parentRegion.regionIndex.Y} suceeded");
					GD.Print($"Attempts: {result.Attempts}");
				}
				GD.Print($"Success: {result.Success}");
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
