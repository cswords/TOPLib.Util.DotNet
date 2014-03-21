using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TOPLib.Util.DotNet.Dynamic.Lang
{
    public abstract class CastedBase
    {
        internal protected Dynamic DynamicObj { get; private set; }

        public CastedBase(Dynamic dynamicObj)
        {
            this.DynamicObj = dynamicObj;
        }
    }
}
