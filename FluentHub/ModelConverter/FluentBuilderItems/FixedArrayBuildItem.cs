﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentHub.ModelConverter.FluentBuilderItems
{
    public class FixedArrayBuildItem<T, VModel> : IBuildItem<T>
        where VModel : class, new()
    {
        private IModelBuilder<VModel> childBuilder;
        private Func<T, IEnumerable<VModel>> getter;
        private Action<T, IEnumerable<VModel>> setter;
        private int loopCount;

        public FixedArrayBuildItem(IModelBuilder<VModel> childBuilder
            , Func<T, IEnumerable<VModel>> getter
            , Action<T, IEnumerable<VModel>> setter
            , int loopCount)
        {
            this.childBuilder = childBuilder;
            this.getter = getter;
            this.setter = setter;
            this.loopCount = loopCount;
        }

        public void Write(T model, BinaryWriter w)
        {
            // 変換する配列をもらう
            var query = getter(model) ?? Enumerable.Empty<VModel>();
            var array = query.ToArray();
            for (int i = 0; i < loopCount; i++)
            {
                var item = null as VModel;
                if (array.Length > i)
                {
                    item = array[i];
                }
                else
                {
                    // 要素数が足りなかったらデフォルト
                    item = new VModel();
                }
                this.childBuilder.ToBytes(w, item);
            }
        }

        public void Read(T model, BinaryReader r, IDictionary<string, object> context)
        {
            var list = new List<VModel>();
            for (var i = 0; i < loopCount; i++)
            {
                var vModel = this.childBuilder.ToModel(r);
                list.Add(vModel);
            }
            setter(model, list);
        }

        public bool CanRead(BinaryReader r, IDictionary<string, object> context)
        {
            for (var i = 0; i < loopCount; i++)
            {
                var result = this.childBuilder.CanToModel(r, context);
                if (result == false)
                    return result;
            }
            return true;
        }
    }
}
