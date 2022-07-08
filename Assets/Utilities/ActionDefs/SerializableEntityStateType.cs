using System;
using UnityEngine;

[Serializable]
public struct SerializableEntityStateType
{
	public SerializableEntityStateType(string typeName)
	{
		_typeName = "";
		this.typeName = typeName;
	}

	public SerializableEntityStateType(Type stateType)
	{
		_typeName = "";
		this.stateType = stateType;
	}

	public string typeName
	{
		get
		{
			return _typeName;
		}
		private set
		{
			stateType = Type.GetType(value);
		}
	}

	public Type stateType
	{
		get
		{
			if (_typeName == null)
			{
				return null;
			}
			Type type = Type.GetType(_typeName);
			if (!(type != null))
			{
				return null;
			}
			return type;
		}
		set
		{
			_typeName = (value != null ? value.AssemblyQualifiedName : "");
		}
	}

	// Token: 0x04003083 RID: 12419
	[SerializeField]
	private string _typeName;
}
