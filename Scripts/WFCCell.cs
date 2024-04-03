namespace hamsterbyte.WFC{
	using System;
	using System.Linq;
	using System.Runtime.InteropServices;
	using Godot;

	public partial class WFCCell{
		private void PrecalculateFrequencies(){
			for( int i = 0; i< rawFrequencies.Length; i++){
				logFrequencies[i] = Math.Log2(rawFrequencies[i]);
			}

			sumOfRawFrequencies = rawFrequencies.Sum();
			sumOfPossibleFrequencies = sumOfRawFrequencies;
			for (int i = 0; i < rawFrequencies.Length; i++){
				sumOfPossibleFrequencyLogFrequencies += Math.Log2(sumOfRawFrequencies) * Math.Log2(rawFrequencies[i]);
			}
		}
		public void UpdateCoordinates(int offsetX, int offsetY, int _regionNumber){
		GD.Print($"region {_regionNumber}: cell updated from ({Coordinates.X + ", " + Coordinates.Y}) to ---> ({(Coordinates.X + offsetX) + ", " + (Coordinates.Y + offsetY)})");	
		Coordinates = new Coordinates(Coordinates.X + offsetX, Coordinates.Y + offsetY);
	
		}

		public void RemoveOption(int i){
			Options[i] = false;
			sumOfPossibleFrequencies -= rawFrequencies[i];
			sumOfPossibleFrequencyLogFrequencies -= logFrequencies[i];
		}

		public double Entropy => Math.Log2(sumOfPossibleFrequencies) - sumOfPossibleFrequencyLogFrequencies/sumOfPossibleFrequencies + entropyNoise;


		private int WeightedRandomIndex(){
			int pointer = 0;
			int randomNumToChooseFromPossible = WFCGrid.Random.Next(0, sumOfPossibleFrequencies);
			for(int i = 0; i < Options.Length; i++){
				if(!Options[i]) continue;
				pointer += rawFrequencies[i];
				if(pointer >= randomNumToChooseFromPossible){
					return i;
				}
			}
			//if returns -1 a contradiction occured
			return -1;
		}
		//used in constructor
		public void DetermineBorderCells(){
			upBorCell= Coordinates.X == 0;
    		leftBorCell = Coordinates.Y == 0;
		}


		public int Collapse(){
			int weightedRandomIndex = WeightedRandomIndex();
			TileIndex = weightedRandomIndex;
			Collapsed = true;
			for(int i = 0; i < Options.Length; i++){
				Options[i] = i == TileIndex;
			}
			return weightedRandomIndex;
		}
	}
}
