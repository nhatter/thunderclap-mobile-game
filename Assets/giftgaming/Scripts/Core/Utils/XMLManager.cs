using UnityEngine;
using System.Collections.Generic;
using System.Xml; 
using System.Xml.Serialization; 
using System.IO;
using System;

public class XMLManager
{
	public static void save<T>(object objectToSerialise, string path)
	{
		XmlSerializer serializer = new XmlSerializer(typeof(T));
		Stream stream = new FileStream(path, FileMode.Create);
		serializer.Serialize(stream, objectToSerialise);
		stream.Close();
	}
	
	public static T load<T>(string path) 
	{
		XmlSerializer serializer = new XmlSerializer(typeof(T));
		Stream stream = new FileStream(path, FileMode.Open);
		T deserialisedObject = (T) serializer.Deserialize(stream);
		stream.Close();
		return deserialisedObject;
	}
	
	public static T loadFromText<T>(string text)
	{
		XmlSerializer serializer = new XmlSerializer(typeof(T));
		return (T) serializer.Deserialize(new StringReader(text));
	}
	
	public static T getObjectsFromXML<T>(string xmlFile) {
		TextAsset textAsset = (TextAsset) Resources.Load(xmlFile, typeof(TextAsset));
		return XMLManager.loadFromText<T>(textAsset.text);
	}
}