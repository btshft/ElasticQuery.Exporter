using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using YamlDotNet.RepresentationModel;

namespace ElasticQuery.Exporter.Lib.Configuration.Yaml
{
    public abstract class YamlVisitor
    {
        private readonly Stack<string> _path;

        protected string Path => ConfigurationPath.Combine(_path.Reverse());

        protected YamlVisitor()
        {
            _path = new Stack<string>();
        }

        public void Visit(YamlDocument document)
        {
            var root = (YamlMappingNode) document.RootNode;
            VisitMapping(root);
        }


        protected abstract void VisitScalar(YamlScalarNode scalar);

        protected virtual void VisitMapping(YamlMappingNode root)
        {
            if (root == null) 
                throw new ArgumentNullException(nameof(root));

            foreach (var (key, node) in root.Children)
            {
                var path = ((YamlScalarNode) key).Value;

                using (EnterPath(path))
                {
                    VisitNode(node);
                }
            }
        }

        protected virtual void VisitNode(YamlNode node)
        {
            switch (node)
            {
                case YamlScalarNode scalarNode:
                    VisitScalar(scalarNode);
                    break;

                case YamlMappingNode mappingNode:
                    VisitMapping(mappingNode);
                    break;

                case YamlSequenceNode sequenceNode:
                    VisitSequence(sequenceNode);
                    break;
            }
        }
        protected virtual void VisitSequence(YamlSequenceNode sequence)
        {
            for (var i = 0; i < sequence.Children.Count; i++)
            {
                using (EnterPath(i.ToString()))
                {
                    VisitNode(sequence.Children[i]);
                }
            }
        }

        protected IDisposable EnterPath(string path)
        {
            return new PathHolder(this, path);
        }

        private class PathHolder : IDisposable
        {
            private readonly YamlVisitor _visitor;

            public PathHolder(YamlVisitor visitor, string path)
            {
                _visitor = visitor;
                _visitor._path.Push(path);
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _visitor._path.Pop();
            }
        }
    }
}