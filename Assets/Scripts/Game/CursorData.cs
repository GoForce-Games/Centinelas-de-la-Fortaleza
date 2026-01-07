using System;
using System.Collections.Generic;

namespace Game
{
    
    
    
    [Serializable]
    public class CursorData
    {
        public string playerName;
        public float posX;  
        public float posY;
        public int playerColorIndex; 
        
        public CursorData() { }
        
        public CursorData(string name, float x, float y, int colorIndex)
        {
            playerName = name;
            posX = x;
            posY = y;
            playerColorIndex = colorIndex;
        }
    }

    
    
    
    [Serializable]
    public class CursorPacket
    {
        public List<CursorData> cursors = new List<CursorData>();
        
        public CursorPacket() { }
        
        public CursorPacket(List<CursorData> cursorList)
        {
            cursors = cursorList;
        }
    }
}
