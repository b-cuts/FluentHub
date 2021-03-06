﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentHub.ModelConverter
{
    public class BaseTypeModelConverter<P, T> : IModelConverter<P>
        where T : P
    {
        private IModelConverter<T> real;

        public BaseTypeModelConverter(IModelConverter<T> real)
        {
            this.real = real;
        }

        public bool CanBytesToModel(IEnumerable<byte> bytes)
        {
            return this.real.CanBytesToModel(bytes);
        }

        public bool CanModelToBytes(object model)
        {
            var isT = model is T;
            if (isT == false)
            {
                return false;
            }
            return this.real.CanModelToBytes(model);
        }

        public byte[] ToBytes(P model)
        {
            return this.real.ToBytes((T)model);
        }

        public Tuple<P, int> ToModel(IEnumerable<byte> bytes)
        {
            var result = this.real.ToModel(bytes);
            return
                Tuple.Create((P)result.Item1, result.Item2);
        }
    }

    
}
