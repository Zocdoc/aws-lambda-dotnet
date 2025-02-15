﻿using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.Lambda.Annotations.SourceGenerator.FileIO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Amazon.Lambda.Annotations.SourceGenerator
{
    internal class SyntaxReceiver : ISyntaxContextReceiver
    {
        public List<MethodDeclarationSyntax> LambdaMethods { get; } = new List<MethodDeclarationSyntax>();

        public List<ClassDeclarationSyntax> StartupClasses { get; private set; } = new List<ClassDeclarationSyntax>();

        public string ProjectDirectory { get; private set; }

        private readonly IFileManager _fileManager;
        private readonly IDirectoryManager _directoryManager;

        public SyntaxReceiver(IFileManager fileManager, IDirectoryManager directoryManager)
        {
            _fileManager = fileManager;
            _directoryManager = directoryManager;
        }

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if(this.ProjectDirectory == null && context.Node is ClassDeclarationSyntax)
            {
                var templateHandler = new CloudFormationTemplateHandler(_fileManager, _directoryManager);
                this.ProjectDirectory = templateHandler.DetermineProjectRootDirectory(context.Node.SyntaxTree.FilePath);
            }

            // any method with at least one attribute is a candidate of function generation
            if (context.Node is MethodDeclarationSyntax methodDeclarationSyntax && methodDeclarationSyntax.AttributeLists.Count > 0)
            {
                // Get the symbol being declared by the method, and keep it if its annotated
                var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
                if (methodSymbol.GetAttributes().Any(attr => attr.AttributeClass.Name == nameof(LambdaFunctionAttribute)))
                {
                    LambdaMethods.Add(methodDeclarationSyntax);
                }
            }

            // any class with at least one attribute is a candidate of Startup class
            if (context.Node is ClassDeclarationSyntax classDeclarationSyntax && classDeclarationSyntax.AttributeLists.Count > 0)
            {
                // Get the symbol being declared by the class, and keep it if its annotated
                var methodSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
                if (methodSymbol.GetAttributes().Any(attr => attr.AttributeClass.Name == nameof(LambdaStartupAttribute)))
                {
                    StartupClasses.Add(classDeclarationSyntax);
                }
            }
        }
    }
}