﻿using System;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Shoko.Server.Repositories.Direct;
using NHibernate;
using NLog;
using Shoko.Models.Server;
using Shoko.Server.Databases;
using Shoko.Server.Repositories;

namespace Shoko.Server.Commands
{
    public abstract class CommandRequestImplementation
    {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // ignoring the base properties so that when we serialize we only get the properties
        // defined in the concrete class

        [XmlIgnore]
        public int CommandRequestID { get; set; }

        [XmlIgnore]
        public int Priority { get; set; }

        [XmlIgnore]
        public int CommandType { get; set; }

        [XmlIgnore]
        public string CommandID { get; set; }

        [XmlIgnore]
        public string CommandDetails { get; set; }

        [XmlIgnore]
        public DateTime DateTimeUpdated { get; set; }

        /// <summary>
        /// Inherited classes to provide the implemenation of how to process this command
        /// </summary>
        public abstract void ProcessCommand();

        public abstract void GenerateCommandID();

        public abstract bool LoadFromDBCommand(CommandRequest cq);

        public abstract CommandRequest ToDatabaseObject();

        public string ToXML()
        {
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", string.Empty);

            XmlSerializer serializer = new XmlSerializer(this.GetType());
            XmlWriterSettings settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true // Remove the <?xml version="1.0" encoding="utf-8"?>
            };
            StringBuilder sb = new StringBuilder();
            XmlWriter writer = XmlWriter.Create(sb, settings);
            serializer.Serialize(writer, this, ns);

            return sb.ToString();
        }

        public void Save()
        {
            using (var session = DatabaseFactory.SessionFactory.OpenSession())
            {
                Save(session);
            }
        }

        public void Save(ISession session)
        {
            CommandRequest crTemp = RepoFactory.CommandRequest.GetByCommandID(session, this.CommandID);
            if (crTemp != null)
            {
                // we will always mylist watched state changes
                // this is because the user may be toggling the status in the client, and we need to process
                // them all in the order they were requested
                if (CommandType == (int) CommandRequestType.AniDB_UpdateWatchedUDP)
                    RepoFactory.CommandRequest.Delete(crTemp);
                else
                    return;
            }

            CommandRequest cri = ToDatabaseObject();
            RepoFactory.CommandRequest.Save(cri);

            switch (CommandType)
            {
                case (int) CommandRequestType.HashFile:
                    ShokoService.CmdProcessorHasher.NotifyOfNewCommand();
                    break;
                case (int) CommandRequestType.ImageDownload:
                case (int) CommandRequestType.ValidateAllImages:
                    ShokoService.CmdProcessorImages.NotifyOfNewCommand();
                    break;
                default:
                    ShokoService.CmdProcessorGeneral.NotifyOfNewCommand();
                    break;
            }
        }

        protected string TryGetProperty(XmlDocument doc, string keyName, string propertyName)
        {
            try
            {
                string prop = doc?[keyName]?[propertyName]?.InnerText.Trim() ?? string.Empty;
                return prop;
            }
            catch
            {
                //BaseConfig.MyAnimeLog.Write("---------------------------------------------------------------");
                //BaseConfig.MyAnimeLog.Write("Error in XMLService.TryGetProperty: {0}-{1}", Utils.GetParentMethodName(), ex.ToString());
                //BaseConfig.MyAnimeLog.Write("keyName: {0}, propertyName: {1}", keyName, propertyName);
                //BaseConfig.MyAnimeLog.Write("---------------------------------------------------------------");
            }

            return string.Empty;
        }
    }
}