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
using Collada141;

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

        NodeBase skeletonFolder = new NodeBase("Skeleton");
        NodeBase meshFolder = new NodeBase("Meshes");
        NodeBase matFolder = new NodeBase("Materials");
        NodeBase texFolder = new NodeBase("Textures");
        NodeBase animFolder = new NodeBase("Animations");

        public void Load(Stream stream)
        {
            stream?.Dispose();

            IOScene = IOManager.LoadScene(FileInfo.FilePath, new ImportSettings());
            Scene = ToGeneric();
            
            Root.AddChild(meshFolder);
            Root.AddChild(matFolder);
            Root.AddChild(skeletonFolder);
            Root.AddChild(texFolder);
            Root.AddChild(animFolder);

            Root.ContextMenus.Add(new MenuItemModel("Add Model", AddModel));
            matFolder.ContextMenus.Add(new MenuItemModel("Remove Duplicate Materials", RemoveDuplicateMaterials));
            ReloadScene();
        }

        private void ReloadScene()
        {
            meshFolder.Children.Clear();
            texFolder.Children.Clear();
            meshFolder.Children.Clear();

            Renders.Clear();
            foreach (var animation in Scene.Animations)
            {
                NodeBase animNode = new NodeBase(animation.Name);
                animNode.Tag = animation;
                animFolder.AddChild(animNode);
            }

            foreach (var mat in IOScene.Materials)
            {
                NodeBase matNode = new NodeBase(mat.Name);
                matNode.Icon = IconManager.MODEL_ICON.ToString();
                matNode.Tag = mat;
                matFolder.AddChild(matNode);
                matNode.CanRename = true;
                matNode.OnHeaderRenamed += delegate
                {
                    //Get assigned polys
                    var polys = GetAssignedPolygons(mat);
                    //Assign with new name
                    AssignMaterial(polys, matNode.Header);
                    //Apply new name
                    mat.Name = matNode.Header;
                };
                matNode.ContextMenus.Add(new MenuItemModel("Rename", () =>
                {
                    matNode.ActivateRename = true;
                }));
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

                foreach (var texture in model.Textures)
                {
                    texFolder.AddChild(new NodeBase(texture.Name) { Tag = texture });
                }
            }
        }

        private void AddModel()
        {
            ImguiFileDialog dlg = new ImguiFileDialog();
            dlg.AddFilter(".dae", "DAE");
            dlg.MultiSelect = true;
            if (dlg.ShowDialog())
            {
                foreach (var file in dlg.FilePaths)
                    AddModel(file);
            }
        }

        private void AddModel(string filePath)
        {
            var scene = IOManager.LoadScene(filePath, new ImportSettings());
            foreach (var mat in scene.Materials)
                if (!IOScene.Materials.Any(x => x.Name == mat.Name))
                    IOScene.Materials.Add(mat);

            foreach (var model in scene.Models)
                IOScene.Models.Add(model);

            Scene = ToGeneric();

            foreach (var render in this.Renders)
                this.RemoveRender(render);

            ReloadScene();
        }

        private void RemoveDuplicateMaterials()
        {
            Dictionary<string, string> materialMaps = new Dictionary<string, string>();
            List<string> duplicateMaterials = new List<string>();

            var materialList = IOScene.Materials.ToList();
            foreach (var mat in materialList)
            {
                string key = mat.DiffuseMap?.Name;
                //Material with existing key
                if (materialMaps.ContainsKey(key))
                {
                    //Get material assigned 
                    string name = materialMaps[key];
                    //Get all polygons assigned to this material
                    var polys = GetAssignedPolygons(mat);
                    //Update and assign new material
                    AssignMaterial(polys, name);
                    //Remove material from scene
                    IOScene.Materials.Remove(mat);
                    //Remove from gui
                    var nodeUI = matFolder.Children.FirstOrDefault(x => x.Header == mat.Name);
                    if (nodeUI != null)
                        matFolder.Children.Remove(nodeUI);

                    duplicateMaterials.Add(mat.Name);
                }
                else
                    materialMaps.Add(key, mat.Name);
            }

            StudioLogger.WriteLine($"Removed {duplicateMaterials.Count} materials! {string.Join('\n', duplicateMaterials)}");
        }

        private void AssignMaterial(List<IOPolygon> polygons, string material)
        {
            foreach (var p in polygons)
                p.MaterialName = material;
        }

        private List<IOPolygon> GetAssignedPolygons(IOMaterial material)
        {
            List<IOPolygon> polygons = new List<IOPolygon>();
            foreach (var model in IOScene.Models)
            {
                foreach (var mesh in model.Meshes)
                {
                    foreach (var p in mesh.Polygons)
                    {
                        if (p.MaterialName == material.Name)
                            polygons.Add(p);
                    }
                }
            }
            return polygons;
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
