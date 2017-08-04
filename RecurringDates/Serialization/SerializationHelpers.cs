﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;

namespace RecurringDates.Serialization
{
    /// <summary>
    /// Serialize/deserialize types implementing IRule (and other types used by the rules, if necessary).
    /// The types are found by reflection in specified assemblies.
    /// If reusing the same RuleSerializer instance, the found types are cached and reused (each assembly is only searched once).
    /// </summary>
    public class RuleSerializer
    {
        private static readonly RuleSerializer _instance = new RuleSerializer();
        
        /// <summary>
        /// A static instance of the serializer. Use this to take advantage of the cached types, 
        /// if serialization / deserialization is done repeatedly.
        /// </summary>
        public static RuleSerializer Instance
        {
            get { return _instance; }
        }

        private readonly RuleTypeCache _typeCache = new RuleTypeCache();

        
        /// <summary>
        /// Hydrate a rule from a string. The rule should only use types defined in this assembly.
        /// </summary>
        /// <param name="serialized"></param>
        /// <returns></returns>
        public IRule Deserialize(string serialized)
        {
            return DeserializeInternal<IRule>(serialized, _ruleAssembly);
        }

        public U Deserialize<U>(string serialized) where U: IRule
        {
            return DeserializeInternal<U>(serialized, _ruleAssembly);
        }

        /// <summary>
        /// Dehydrate a rule to a string. The rule should only use types defined in this assembly.
        /// </summary>
        /// <param name="rule"></param>
        /// <returns></returns>
        public string Serialize(IRule rule)
        {
            return SerializeInternal(rule, _ruleAssembly);
        }

        /// <summary>
        /// Hydrate a rule to a string. Use for custom rules defined outside this project.
        /// Specify additional assemblies to search for (custom) rules. 
        /// This assembly is searched automatically and does not need to be specified.
        /// When serializing, all types implementing IRule in the specified assemblies are used, if necessary.
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="ruleTypesAssemblies"></param>
        /// <returns></returns>
        public string Serialize(IRule rule, params Assembly[] ruleTypesAssemblies)
        {
            // ReSharper disable once CoVariantArrayConversion
            return SerializeInternal(rule, GetLoadList(ruleTypesAssemblies));
        }

        /// <summary>
        /// Hydrate a rule to a string. Specify additional types to use 
        /// (IRule custom implementation and other types used by the custom rules).
        /// The types that implement IRule in this assembly are added automatically.
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        public string Serialize(IRule rule, params Type[] types)
        {
            // ReSharper disable once CoVariantArrayConversion
            return SerializeInternal(rule, GetLoadList(types));
        }

        /// <summary>
        /// Hydrate a rule to a string. Use for custom rules defined outside this project.
        /// Specify additional assemblies to search for (custom) rules and/or specify additional types to use 
        /// (IRule custom implementation and other types used by the custom rules). 
        /// This assembly is searched automatically and does not need to be specified.
        /// When serializing, all types implementing IRule in the specified assemblies, and all other specified types, are used, if necessary.
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="ruleTypesAssemblies"> list of Assembly or Type elements. 
        /// An exception is thrown if any element of another type is specified</param>
        /// <returns></returns>
        public string Serialize(IRule rule, params object[] ruleTypesAssemblies)
        {
            return SerializeInternal(rule, GetLoadList(ruleTypesAssemblies));
        }

        private string SerializeInternal(IRule rule, params object[] ruleTypesAssemblies)
        {

            var serializer = GetSerializer(ruleTypesAssemblies);
            StringWriter sw = new StringWriter();

            XmlWriter xw = XmlWriter.Create(sw);

            serializer.WriteObject(xw, rule);
            xw.Flush();
            return sw.ToString();
        }

        private static readonly Assembly _ruleAssembly = typeof (IRule).GetTypeInfo().Assembly;

        private static object[] GetLoadList(object[] parameters)
        {
            var list = new object[1 + parameters.Length];
            list[0] = _ruleAssembly;
            int crtPos = 1;
            foreach (var assembly in parameters)
            {
                list[crtPos++] = assembly;
            }
            return list;
        }

        public IRule Deserialize(string serialized, params Assembly[] ruleTypesAssemblies)
        {
            // ReSharper disable once CoVariantArrayConversion
            return DeserializeInternal<IRule>(serialized, GetLoadList(ruleTypesAssemblies));
        }

        public IRule Deserialize(string serialized, params Type[] ruleTypesAssemblies)
        {
            // ReSharper disable once CoVariantArrayConversion
            return DeserializeInternal<IRule>(serialized, GetLoadList(ruleTypesAssemblies));
        }

        public IRule Deserialize(string serialized, params object[] ruleTypesAssemblies)
        {
            return DeserializeInternal<IRule>(serialized, GetLoadList(ruleTypesAssemblies));
        }

        private U DeserializeInternal<U>(string serialized, params object[] ruleTypesAssemblies) where U: IRule
        {
            var serializer = GetSerializer(ruleTypesAssemblies);

            var xmlReader = XmlReader.Create(new StringReader(serialized));
            var obj = serializer.ReadObject(xmlReader);

            return (U)obj;
        }

        private DataContractSerializer GetSerializer(object[] ruleTypesAssemblies)
        {
            var knownTypes =  GetKnownTypes(ruleTypesAssemblies);

            DataContractSerializer serializer = new DataContractSerializer(typeof(IRule), knownTypes);
            return serializer;
        }

        private IEnumerable<Type> GetKnownTypes(object[] ruleTypesAssemblies)
        {
            var assemblies = ruleTypesAssemblies.OfType<Assembly>().ToList();
            var types = ruleTypesAssemblies.OfType<Type>().ToList();

            var others = ruleTypesAssemblies.Except(assemblies).Except(types)
                .ToList();

            if (others.Any())
            {
                throw new ArgumentException(
                    "ruleTypesAssemblies should only contain items of type System.Assembly or System.Type, but found: " +
                    others.First().GetType());
            }

            foreach (var assembly in assemblies)
            {
                _typeCache.Load(assembly);
            }

            var knownTypes = _typeCache.Concat(types);
            return knownTypes;
        }
    }
}
