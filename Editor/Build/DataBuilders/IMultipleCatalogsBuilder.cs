using System.Collections.Generic;
using UnityEngine;

public interface IMultipleCatalogsBuilder
{
	List<CatalogContentGroup> AdditionalCatalogs
	{
		get; set;
	}
}
