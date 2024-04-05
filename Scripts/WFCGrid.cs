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
	using System.Text;
    public partial class WFCGrid{

		public int numattempt = 0;
		public int pushes;
		public int pops;
		public string pushPopHistory;
		
		public WFCCell getCell(int row, int col){
			return cells[row,col];
		}
		public void updateBorderCellList(BorderCellUpdate _borCellUpdate){
			bCellUpdates.Add(_borCellUpdate);
		}
		public void ProcessBorderCellUpdates(){
			if (bCellUpdates.Count == 0) return;
			GD.Print($"region({parentRegion.regionIndex.X},{parentRegion.regionIndex.Y}) is beginning to ProcessBCells");
			foreach (var update in bCellUpdates)
			{
				// Access the cell at the specified coordinates
        		var currentCell = cells[update.X, update.Y];


				 foreach (var option in update.OptionsRemovedList)
					{
						if (currentCell.Options[option]) //if its true then remove it, if its somehow false then ignore
							{
								currentCell.RemoveOption(option);
							}
					}
					entropyHeap.Push(new EntropyCoordinates(){
						Coordinates = currentCell.Coordinates,
						Entropy = currentCell.Entropy
					});
				// Sample functionality: Print the X, Y, and size of OptionsRemovedList
				GD.Print($"Processing update for cell ({update.X}, {update.Y}) with {update.OptionsRemovedList.Count} options removed");
				StringBuilder notRemovedIndexes = new StringBuilder();
				for (int i = 0; i < 14; i++)
					{
						if (!update.OptionsRemovedList.Contains(i))
						{
							notRemovedIndexes.Append(i + " ");
						}
					}
				GD.Print($"Indexes not removed for cell ({update.X}, {update.Y}): {notRemovedIndexes}");

			}
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
			if(_coords.Y == 0){
				GD.Print($"Cell({_coords.X},{_coords.Y}) collapsed as tile index { collapsedIndex}");
			}
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
						if(current.X < 0 || current.Y < 0) continue;//SKIP CELLS TO THE LEFT and UP , THEY ARE ALREADY GENERATED
						current = current.Wrap(Width, Height);
						if(tempCurrent != current){ //it wrapped! now find if it wraps left or right
							if( tempCurrent.X != current.X) {cellType = 'l';}//lower region  //possible bug
							else if(tempCurrent.Y != current.Y){cellType = 'r';} //right region
						}
						
					} else if (!IsInBounds(current)){
						continue;
					}

					WFCCell currentCell;
					BorderCellUpdate borCellUpdate= new BorderCellUpdate();
					if (cellType == 'u')
					{
						currentCell = cells[current.X, current.Y];
					}
					else//its borrowed from another region
					{
						borCellUpdate.X = current.X; borCellUpdate.Y = current.Y;
						borCellUpdate.OptionsRemovedList = new List<int>();
						borrowed = true;
						//get list of region it needs to go to
						
						//currentCell = parentRegion.getCellFromRegionManager(cellType, current.X, current.Y);
						currentCell = new WFCCell(new Coordinates(-1,-1), rawFrequencies);//dummy cell
						//GD.Print($"region {parentRegion.regionIndex} requested a cell from region {parentRegion.lowerNeighbor}");
						//GD.Print($"Original {update.Coordinates.X},{update.Coordinates.Y} needed cell {currentCell.Coordinates.X},{currentCell.Coordinates.Y}");
					}
					//if (currentCell.Coordinates.X == -1) continue;  //requested a cell from aregion that doesnt exist
					if(currentCell.Collapsed) continue;

					//List<int> indexesToRemove = new List<int>(); //for borrowed cells only

					for(int o = 0; o < adjacencyRules.GetLength(2); o++){
						if(adjacencyRules[update.TileIndex, d, o] == 0 && currentCell.Options[o]){
							if (borrowed){
									borCellUpdate.OptionsRemovedList.Add(o); //create a list that will be passed to foreign cells grid,
															// with each reset, the list will be iterated and all cell options updated
								}else{
									currentCell.RemoveOption(o);
								}
						}
					}
					
					if(entropyHeap.isHeapFull()){
						entropyHeap = new EntropyHeap(Width * Height);
					}
					if(borrowed){
						//sent update to corresponding region
						parentRegion.SendBorderCellUpdate(cellType,borCellUpdate);
						//need to create funciton in wfcgrid that will iterate through its update list and push the corresponding cells 
						//to entropy heap to be handled.
					}else{
						if (currentCell.Entropy < -100 && currentCell.Entropy > 100){
							//stop
						}

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
				if(parentRegion.regionIndex.Y == 1){
					//stop lets trace
				}
				Busy = true;
				Stopwatch timer = Stopwatch.StartNew();
				for(int i  = 0; i < _maxAttempts; i++){
					ProcessBorderCellUpdates();
					pushes = 0;
					pops = 0;
					pushPopHistory = " ";
					currentAttempt++;
					//GD.Print($"region ({parentRegion.regionIndex.X}, {parentRegion.regionIndex.Y}) has begun a generation attempt");
					
					if(entropyHeap.getSize() != 0){
						GD.Print("entropy heap isnt empty before new attempt ");
					}
					WFCCell cell = cells.Random(); //needed?

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
