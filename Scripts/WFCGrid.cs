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
		public WFCCell getCell(int row, int col){
			return cells[row,col];
		}
		private EntropyCoordinates Observe(){
			
			while(!entropyHeap.IsEmpty){
				EntropyCoordinates coords = entropyHeap.Pop();
				pops++;
				if(!cells[coords.Coordinates.X, coords.Coordinates.Y].Collapsed){ 
					return coords;}
			}
			//GD.Print("Heap was emptied!");
			//if code gets here, that means the heap was overflowing. so it was emptied and arrived here. notifying
			//that a new attempt at generation is needed
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
				char cellType;
				bool borrowed;
				Coordinates[] cardinals = Coordinates.Cardinals;
				for(int d = 0; d < adjacencyRules.GetLength(1); d++){
					borrowed = false;
					cellType = 'u'; //unchanged
					Coordinates tempCurrent = new Coordinates();
					Coordinates current = cardinals[d] + update.Coordinates;
					tempCurrent = current;
					if (_wrap){
						if(update.Coordinates.X == 0 && update.Coordinates.Y == 0 && d ==0){
							//we are on top row, check coords
						}
						if(current.X < 0 || current.Y < 0) continue;//SKIP CELLS TO THE LEFT, THEY ARE ALREADY GENERATED
						current = current.Wrap(Width, Height);
						if(tempCurrent != current){ //it wrapped! now find if it wraps left or right
							if( tempCurrent.X != current.X) {cellType = 'l';}//lower region
							else if(tempCurrent.Y != current.Y){cellType = 'r';} //right region
						}
						
					} else if (!IsInBounds(current)){
						continue;
					}

					WFCCell currentCell;
					switch(cellType){
						case 'u': //unchanged
							currentCell = cells[current.X, current.Y];
						break;
						case 'l': //lower
							currentCell = parentRegion.getCellFromRegionManager(cellType, current.X, current.Y);
							borrowed = true;
							GD.Print($"region {parentRegion.regionIndex} requestd a cell from region {parentRegion.lowerNeighbor}");
							GD.Print($"Original {update.Coordinates.X},{update.Coordinates.Y} needed cell { currentCell.Coordinates.X },{ currentCell.Coordinates.Y}");
						break;
						case 'r'://right
						borrowed = true;
							currentCell = parentRegion.getCellFromRegionManager(cellType, current.X, current.Y);
						break;
						 default:
						// Handle unexpected cellType values
						// For example, you can throw an exception or set currentCell to a default value
						currentCell = new WFCCell(new Coordinates(-1, -1), new int[0]);
						break;
					}
					if (currentCell.Coordinates.X == -1) continue;  //requested a cell from aregion that doesnt exist
					if(currentCell.Collapsed) continue;
					for(int o = 0; o < adjacencyRules.GetLength(2); o++){
						if(adjacencyRules[update.TileIndex, d, o] == 0 && currentCell.Options[o]){
							currentCell.RemoveOption(o);
							if(borrowed){
								GD.Print($"Cell({currentCell.Coordinates.X},{currentCell.Coordinates.Y})'s had an option removed ");
							}
							
						}
					}
					
					if(entropyHeap.isHeapFull()){
						entropyHeap = new EntropyHeap(Width * Height);
					}
					if(borrowed == false){
					entropyHeap.Push(new EntropyCoordinates(){
						Coordinates = currentCell.Coordinates,
						Entropy = currentCell.Entropy
					});
					}

					pushes++;
					pushPopHistory += "U";
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
					//GD.Print($"region ({parentRegion.regionIndex.X}, {parentRegion.regionIndex.Y}) has begun a generation attempt");
					
					if(entropyHeap.getSize() != 0){
						GD.Print("entropy heap isnt empty before new attempt ");
					}
					WFCCell cell = cells.Random();

					entropyHeap.Push(new EntropyCoordinates(){
						Coordinates = cell.Coordinates,
						Entropy = cell.Entropy
					});
					pushes++;
					pushPopHistory += "U";

					while (remainingUncollapsedCells > 0){
						EntropyCoordinates e = Observe();
						if (e.Coordinates.X == -1 && e.Coordinates.Y == -1){ //handle heap overflow bug
							validCollapse = false;
 							break;//reset
						}
						Collapse(e.Coordinates);
						Propagate(_wrap);
					}
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
				if(!result.Success){
					ShowResultMetrics(result);
				}
				else{
					GD.Print($"Region ({parentRegion.regionIndex.X},{parentRegion.regionIndex.Y}) -- LowerNeighbor:{parentRegion.lowerNeighbor}   RightNeighbor:{parentRegion.rightNeighbor} ");
					GD.Print($"Region ({parentRegion.regionIndex.X},{parentRegion.regionIndex.Y}) succeeded. Attempts: {result.Attempts}");
					//GD.Print($"Region ({parentRegion.regionIndex.X},{parentRegion.regionIndex.Y})'s final push count: {pushes}-- pops: {pops}");
					GD.Print("------------------------------------");
				}
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
