﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.EnterpriseLibrary.Common.Utility;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using System.Collections.Specialized;
using System.Configuration;
using System.ComponentModel;
using Console.Wpf.ComponentModel.Editors;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration.Design;

namespace Console.Wpf.ViewModel
{
    public class CustomAttributesPropertyExtender : ElementViewModel, IElementExtendedPropertyProvider
    {
        IServiceProvider serviceProvider;
        public CustomAttributesPropertyExtender(IServiceProvider serviceProvider)
            :base(null, (ConfigurationElement)null, new Attribute[]{new EnvironmentalOverridesAttribute(false)})
        {
            this.serviceProvider = serviceProvider;
        }

        public bool CanExtend(ElementViewModel subject)
        {
            return typeof(ICustomProviderData).IsAssignableFrom(subject.ConfigurationType);
        }

        public IEnumerable<Property> GetExtendedProperties(ElementViewModel subject)
        {
            yield return new CustomAttributesProperty(serviceProvider, subject, TypeDescriptor.GetProperties(subject.ConfigurationType).OfType<PropertyDescriptor>().Where(x => x.Name == "Attributes").FirstOrDefault());
        }

        public override Type ConfigurationType
        {
            get
            {
                return typeof(object);
            }
        }

        public override string Path
        {
            get
            {
                return "<custom attributes>";
            }
        }



        private class CustomAttributesProperty : Property
        {
            ElementViewModel subject;
            CustomAttributesEditor editor = new CustomAttributesEditor();

            public CustomAttributesProperty(IServiceProvider serviceProvider, ElementViewModel subject, PropertyDescriptor declaringProperty)
                : base(serviceProvider, subject.ConfigurationElement, declaringProperty, new Attribute[]{new EnvironmentalOverridesAttribute(false)})
            {
                this.subject = subject;
            }

            public override string PropertyName
            {
                get
                {
                    return "Attributes";
                }
            }
            public override bool ReadOnly
            {
                get
                {
                    return false;
                }
            }

            public override bool HasEditor
            {
                get
                {
                    return true;
                }
            }

            public override EditorBehavior EditorBehavior
            {
                get
                {
                    return EditorBehavior.DropDown;
                }
            }

            public override System.Windows.FrameworkElement Editor
            {
                get
                {
                    return editor;
                }
            }
        }
    }
}