using System;

public class WFCRegion {
    // Define properties to represent the region's position, size, and tile data
    public int X { get; private set; } // X-coordinate of the top-left corner of the region
    public int Y { get; private set; } // Y-coordinate of the top-left corner of the region
    public int Width { get; private set; } // Width of the region in tiles
    public int Height { get; private set; } // Height of the region in tiles
    public int[,] Tiles { get; private set; } // 2D array to store tile data within the region

    // Constructor to initialize the region with its position, size, and tile data
    public WFCRegion(int x, int y, int width, int height) {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Tiles = new int[Width, Height]; // Initialize tile data array
    }

    // Method to set the tile at a specific position within the region
    public void SetTile(int x, int y, int tileIndex) {
        // Ensure that the specified position is within the bounds of the region
        if (x < 0 || x >= Width || y < 0 || y >= Height) {
            throw new ArgumentOutOfRangeException("Position is out of bounds");
        }

        // Set the tile at the specified position
        Tiles[x, y] = tileIndex;
    }

    // Method to get the tile at a specific position within the region
    public int GetTile(int x, int y) {
        // Ensure that the specified position is within the bounds of the region
        if (x < 0 || x >= Width || y < 0 || y >= Height) {
            throw new ArgumentOutOfRangeException("Position is out of bounds");
        }

        // Return the tile at the specified position
        return Tiles[x, y];
    }
}