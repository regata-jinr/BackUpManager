using System;
namespace BackUpDB
{
  public class Program
  {
    private static void Main()
    {
      var bm = new BackUpManager();
      bm.Notify(bm.BackUpDataBase(), bm.MoveFileToGDriveFolder());
    } // Main
  } // class Program
} //namespace BackUpDB
