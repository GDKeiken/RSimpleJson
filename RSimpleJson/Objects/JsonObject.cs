﻿using System;
using System.Collections.Generic;

namespace RSimpleJson.Objects
{
    [Serializable]
    public class JsonObject : Dictionary<string, object>, IJsonObject
	{
	}
}
