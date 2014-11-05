﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Common.Logging;

namespace Npgsql.TypeHandlers
{
    internal class TypeHandlerRegistry
    {
        Dictionary<int, TypeHandler> _oidIndex;

        static List<TypeHandler> _typeHandlers;
        static readonly TypeHandler UnknownTypeHandler = new UnknownTypeHandler();
        static readonly TypeHandlerRegistry EmptyRegistry = new TypeHandlerRegistry();

        static readonly ILog _log = LogManager.GetCurrentClassLogger();

        TypeHandlerRegistry()
        {
            _oidIndex = new Dictionary<int, TypeHandler>();
        }

        /// <summary>
        /// Looks up a type handler by its Postgresql type's OID.
        /// </summary>
        /// <param name="oid">A Postgresql type OID</param>
        /// <returns>A type handler that can be used to encode and decode values.</returns>
        internal TypeHandler this[int oid]
        {
            get
            {
                TypeHandler result;
                if (!_oidIndex.TryGetValue(oid, out result)) {
                    result = UnknownTypeHandler;
                }
                return result;
            }
            set { _oidIndex[oid] = value; }
        }

        static internal void Load(NpgsqlConnector connector)
        {
            // TODO: Cache like in previous implementation

            // Below we'll be sending in a query to load OIDs from the backend, but parsing those results will depend
            // on... the OIDs. To solve this chicken and egg problem, set up an empty type handler registry that will
            // enable us to at least read strings
            connector.TypeHandlerRegistry = EmptyRegistry;

            var result = new TypeHandlerRegistry();
            var inList = new StringBuilder();
            var nameIndex = new Dictionary<string, TypeHandler>();

            foreach (var typeInfo in _typeHandlers)
            {
                foreach (var pgName in typeInfo.PgNames) {
                    nameIndex.Add(pgName, typeInfo);
                    inList.AppendFormat("{0}'{1}'", ((inList.Length > 0) ? ", " : ""), pgName);
                }

                //do the same for the equivalent array type.

                // TODO: Implement arrays
                //nameIndex.Add("_" + typeInfo.Name, ArrayTypeInfo(typeInfo));
                //inList.Append(", '_").Append(typeInfo.Name).Append('\'');
            }

            using (var command = new NpgsqlCommand(String.Format("SELECT typname, oid FROM pg_type WHERE typname IN ({0})", inList), connector))
            {
                // TODO: Implement Sequential, SingleResult, etc.
                //using (var dr = command.GetReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                using (var dr = command.GetReader())
                {
                    while (dr.Read())
                    {
                        var handler = nameIndex[dr[0].ToString()];
                        var oid = Convert.ToInt32(dr[1]);
                        Debug.Assert(handler.Oid == -1);
                        // TODO: Check if we actually need the OID here (we will for write).
                        // If so we need to create instances of handlers per-connector, since OIDs may differ
                        //handler.Oid = oid;
                        result[oid] = handler;
                    }
                }
            }

            /*foreach (var notFound in _typeHandlers.Where(t => t.Oid == -1)) {
                _log.WarnFormat("Could not find type {0} in pg_type", notFound.PgNames[0]);
            }*/

            connector.TypeHandlerRegistry = result;
        }

        static void DiscoverTypeHandlers()
        {
            _typeHandlers = typeof(TypeHandlerRegistry).Assembly.DefinedTypes
                .Where(t => !t.IsAbstract &&
                             t.IsSubclassOf(typeof(TypeHandler)) &&
                             t != typeof(UnknownTypeHandler))
                .Select(Activator.CreateInstance)
                .Cast<TypeHandler>()
                .ToList();
        }

        static TypeHandlerRegistry()
        {
            DiscoverTypeHandlers();
        }
    }
}
