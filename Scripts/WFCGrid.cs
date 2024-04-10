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
	
		
		public WFCCell getCell(int row, int col){
			return cells[row,col];
		}
		public void updateBorderCellList(BorderCellUpdate _borCellUpdate){
			bCellUpdates.Add(_borCellUpdate);
		}
		public void ProcessBorderCellUpdates(){
			if (bCellUpdates.Count == 0) return;
			GD.Print($" WFCGrid: region({parentRegion.regionIndex.X},{parentRegion.regionIndex.Y}) is beginning to ProcessBCells");
			foreach (var update in bCellUpdates)
			{
				// Access the cell at the specified coordinates
				var currentCell = cells[update.X, update.Y];

			//GD.Print($" WFCGrid: Processing update for cell ({update.X}, {update.Y}) with {update.OptionsRemovedList.Count} options removed");
				 foreach (var option in update.OptionsRemovedList)
					{
						
						if (currentCell.Options[option]) //if its true then remove it, if its somehow false then ignore
							{
								currentCell.RemoveOption(option);
							}
					}

					// entropyHeap.Push(new EntropyCoordinates(){
					// 	Coordinates = currentCell.Coordinates,
					// 	Entropy = currentCell.Entropy
					// });
				// Sample functionality: Print the X, Y, and size of OptionsRemovedList
				
				int trueCount = 0;
				foreach (var option in currentCell.Options)
				{
					if (option)
					{
						trueCount++;
					}
				}

				if (trueCount != update.sentIndexesRemaining)
				{
					// Add a breakpoint here for debugging
					// You can add a log message or other debugging information here
					//GD.Print($"WFCGrid: Warning - true count ({trueCount}) does not match sentIndexesRemaining ({update.sentIndexesRemaining})");
				}
				
			}
		}
		public void AppendToCandidateList(BorderCellUpdate borCellUpdate, char cellType)
			{
				if (cellType == 'l')
				{
					lowerNeighborbCellUpdatesCandidateList.Add(borCellUpdate);
				}
				else if (cellType == 'r')
				{
					rightNeighborbCellUpdatesCandidateList.Add(borCellUpdate);
				}
			}
		public void SendUpdatesToParentRegion()
		{
			foreach (var update in rightNeighborbCellUpdatesCandidateList)
			{
				parentRegion.SendBorderCellUpdate('r', update);
			}

			foreach (var update in lowerNeighborbCellUpdatesCandidateList)
			{
				parentRegion.SendBorderCellUpdate('l', update);
			}
		}
		private EntropyCoordinates Observe(){
			
			while(!entropyHeap.IsEmpty){
				EntropyCoordinates coords = entropyHeap.Pop();
				
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
				//GD.Print($"Cell({_coords.X},{_coords.Y}) collapsed as tile index { collapsedIndex}");
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
					List<int> tempList = new List<int>();
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
						if(borCellUpdate.X == 0 && borCellUpdate.Y ==0 && cellType == 'r'){
							//looking at 0,0 for region to the right 
						}
						borCellUpdate.OptionsRemovedList = new List<int>();
						borCellUpdate.determiningTileIndex = update.TileIndex;
						borrowed = true;
						currentCell = new WFCCell(new Coordinates(-1,-1), rawFrequencies);//dummy cell
					}
				
					if(currentCell.Collapsed) continue;

					//List<int> indexesToRemove = new List<int>(); //for borrowed cells only

					for(int o = 0; o < adjacencyRules.GetLength(2); o++){
						if(adjacencyRules[update.TileIndex, d, o] == 0 && currentCell.Options[o]){
							if (borrowed){
									borCellUpdate.OptionsRemovedList.Add(o); //create a list that will be passed to foreign cells grid,
															// with each reset, the list will be iterated and all cell options updated
								}else{
									currentCell.RemoveOption(o);
									tempList.Add(o);
								}
						}
					}
	
					if(entropyHeap.isHeapFull()){
						entropyHeap = new EntropyHeap(Width * Height);
					}
					if(borrowed){
						//sent update to corresponding region
						
						StringBuilder notRemovedIndexes = new StringBuilder();
						for (int i = 0; i < 14; i++)
							{
								if (!borCellUpdate.OptionsRemovedList.Contains(i))
								{
									notRemovedIndexes.Append(i + " ");
								}
							}
							if(borCellUpdate.X == 0 && borCellUpdate.Y ==0){
						GD.Print($"WFCGrid: {cellType} cell ({borCellUpdate.X}, {borCellUpdate.Y}):  remaining indexes:{notRemovedIndexes}");
					}
						borCellUpdate.sentIndexesRemaining = notRemovedIndexes.ToString()
													.Split(new[] { ' ', '{', '}', ',' }, StringSplitOptions.RemoveEmptyEntries)
													.Length;
						
						AppendToCandidateList(borCellUpdate, cellType);
					}else{
						if (currentCell.Entropy < -100 && currentCell.Entropy > 100){
							throw new Exception("Entropy value out of range.");
						}

						entropyHeap.Push(new EntropyCoordinates(){
						Coordinates = currentCell.Coordinates,
						Entropy = currentCell.Entropy
					});
					}
				

					
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
					//reinitialize my candidate lists
					rightNeighborbCellUpdatesCandidateList = new List<BorderCellUpdate>();
					lowerNeighborbCellUpdatesCandidateList = new List<BorderCellUpdate>();
					
					currentAttempt++;

					if(entropyHeap.getSize() != 0){
						GD.Print("entropy heap isnt empty before new attempt ");
					}
					WFCCell cell = cells.Random(); //needed?

					entropyHeap.Push(new EntropyCoordinates(){
						Coordinates = cell.Coordinates,
						Entropy = cell.Entropy
					});
				

					while (remainingUncollapsedCells > 0){
						EntropyCoordinates e = Observe();
						if(parentRegion.regionIndex.Y == 1){
							if(e.Coordinates.Y ==0){
								//stop and watch
							}
						}
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
				SendUpdatesToParentRegion(); //send borderUpdatesto neighbor regions (if region DNE - does nothing)

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
