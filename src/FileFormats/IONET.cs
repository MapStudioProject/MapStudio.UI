using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using IONET;
using IONET.Core;
using IONET.Core.Model;
using IONET.Core.Skeleton;
using IONET.Helpers;
using Toolbox.Core;
using GLFrameworkEngine;
using Toolbox.Core.ViewModels;
using UIFramework;
using IONET.Collada.FX.Rendering;

namespace MapStudio.UI
{
    public class IoNetFile : FileEditor, IFileFormat, IModelSceneFormat
    {
        public bool CanSave { get; set; } = true;

        public string[] Description { get; set; } = new string[] { "DAE" };
        public string[] Extension { get; set; } = new string[] { "*.dae" };

        public File_Info FileInfo { get; set; }

        public bool Identify(File_Info fileInfo, Stream stream)
        {
            return fileInfo.Extension == ".dae" ||
                   fileInfo.Extension == ".smd" ||
                   fileInfo.Extension == ".fbx" ||
                   fileInfo.Extension == ".obj";
        }

        public IOScene IOScene;

        public STGenericScene Scene;

        List<GenericModelRender> Renders = new List<GenericModelRender>();

        public void Load(Stream stream)
        {
            stream?.Dispose();

            IOScene = IOManager.LoadScene(FileInfo.FilePath, new ImportSettings());

            Scene = ToGeneric();

            NodeBase skeletonFolder = new NodeBase("Skeleton");
            NodeBase meshFolder = new NodeBase("Meshes");
            NodeBase texFolder = new NodeBase("Textures");
            NodeBase animFolder = new NodeBase("Animations");

            Root.AddChild(meshFolder);
            Root.AddChild(skeletonFolder);
            Root.AddChild(texFolder);
            Root.AddChild(animFolder);

            foreach (var animation in Scene.Animations)
            {
                NodeBase animNode = new NodeBase(animation.Name);
                animNode.Tag = animation;
                animFolder.AddChild(animNode);
            }

            Renders.Clear();
            foreach (var model in Scene.Models)
            {
                var modelRender = new GenericModelRender(model);
                this.AddRender(modelRender);
                Renders.Add(modelRender);

                var roots = CreateBoneTree(model.Skeleton);
                foreach (var bone in roots)
                    skeletonFolder.AddChild(bone);

                foreach (var meshRender in modelRender.Meshes)
                    meshFolder.AddChild(meshRender.UINode);

                foreach (var texture in model.Textures) {
                    texFolder.AddChild(new NodeBase(texture.Name) { Tag = texture });
                }
            }
        }

        public override List<DockWindow> PrepareDocks()
        {
            var docks = base.PrepareDocks();
            return docks;
        }

        public NodeBase[] CreateBoneTree(STSkeleton skeleton)
        {
            List<NodeBase> nodes = new List<NodeBase>();
            foreach (var bone in skeleton.Bones)
                nodes.Add(new NodeBase(bone.Name) { Tag = bone });

            List<NodeBase> roots = new List<NodeBase>();
            foreach (var bone in skeleton.Bones)
            {
                int index = skeleton.Bones.IndexOf(bone);
                if (bone.ParentIndex != -1)
                    nodes[bone.ParentIndex].AddChild(nodes[index]);
                else
                    roots.Add(nodes[index]);
            }
            return roots.ToArray();
        }

        public void Save(Stream stream) {
            stream.Dispose();

            Export(Scene, FileInfo.FilePath);
        }

        public STGenericScene ToGeneric() => StudioConversion.ToGeneric(IOScene);

        public void Export(STGenericScene genericScene, string filePath)
        {
           var scene = StudioConversion.FromGeneric(genericScene);
            for (int i = 0; i < scene.Models.Count; i++)
            {
                for (int j = 0; j < scene.Models[i].Meshes.Count; j++)
                {
                    var mesh = scene.Models[i].Meshes[j];
                    var transform = Renders[i].Transform.TransformMatrix * Renders[i].Meshes[j].Transform.TransformMatrix;
                    mesh.TransformVertices(Matrix4Extension.ToNumerics(transform));
                }
            }

            IOManager.ExportScene(scene, filePath, new ExportSettings()
            {
                BlenderMode = true,
                ExportMaterialInfo = true,
                ExportTextureInfo = true,
            });
        }
    }
}
