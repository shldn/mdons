using UnityEngine;
using System;
using System.Text;
using System.IO;
using System.Collections;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;


public class ZipHelper {
	
	static public void Unzip(string file, string destfolder)
	{
		using (ZipInputStream s = new ZipInputStream(File.OpenRead(file))) 
		{
			ZipEntry theEntry;
			while ((theEntry = s.GetNextEntry()) != null)
			{
				Debug.Log(theEntry.Name);
				
				string directoryName = Path.GetDirectoryName(theEntry.Name);
				string fileName      = Path.GetFileName(theEntry.Name);
				
				// create directory
				if ( directoryName.Length > 0 ) {
					Directory.CreateDirectory(destfolder + directoryName);
				}
				
				if (fileName != String.Empty) {
					using (FileStream streamWriter = File.Create(destfolder + theEntry.Name))
					{
						int size = 2048;
						byte[] data = new byte[2048];
						while (true) {
							size = s.Read(data, 0, data.Length);
							if (size > 0) {
								streamWriter.Write(data, 0, size);
							} else {
								break;
							}
						}
					}
				}
			}
		}
	}
	
	
}
