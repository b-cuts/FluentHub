﻿using FluentHub.IO;
using FluentHub.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentHub.Validation
{
    public class ValidationModelContext<AppIF> : IIOContext<AppIF>
    {
        private IIOContext<AppIF> context;
        private ILogger logger;
        private Dictionary<Type, IModelValidator> validators;
        private object syncObject = new object();

        public ValidationModelContext(
            IIOContext<AppIF> context
            , ILogger logger
            , params IModelValidator[] validators)
        {
            this.context = context;
            this.logger = logger;
            this.validators = validators.ToDictionary(x=>x.GetType().BaseType.GetGenericArguments()[0],x=>x);
            this.context.Received += Context_Received;
        }

        private void Context_Received(object sender, EventArgs e)
        {
            if (Received == null)
            {
                return;
            }
            Received(this, EventArgs.Empty);
        }

        public bool CanUse => this.context.CanUse;

        public bool IsAny => this.context.IsAny;

        public event EventHandler Received;

        public void Dispose()
        {
            this.context.Dispose();
        }

        public AppIF Read()
        {
            lock (this.syncObject)
            {
                return ReadWithValidate(this.context.Read);
            }
        }


        public AppIF Read(Func<AppIF, bool> predicate)
        {
            lock (this.syncObject)
            {
                return ReadWithValidate(() => this.context.Read(predicate));
            }
        }


        AppIF ReadWithValidate(Func<AppIF> read)
        {
            var model = default(AppIF);
            do
            {
                model = read();
                if (model == null)
                {
                    break;
                }

                var modelType = model.GetType();
                if (this.validators.ContainsKey(modelType) == false)
                {
                    break;
                }

                var validator = validators[modelType];
                var result = validator.Validate(model);
                if (result)
                {
                    break;
                }

                this.logger.Info($"not validate {modelType.Name} message");
            }
            while (true);
            return model;
        }

        public void Write(AppIF model)
        {
            lock (this.syncObject)
            {
                this.context.Write(model);
            }
        }
    }
}
