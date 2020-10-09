using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NUnit.Framework;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.Entities.CodeGen;
using Unity.Entities.CodeGen.Tests;

namespace Unity.Entities.Hybrid.CodeGen.Tests
{
    [TestFixture]
    class BufferElementDataCompileTimeTests : PostProcessorTestBase
    {
        [Test]
        public void WrapAroundMultipleValuesThrowsError()
        {
            AssertProducesError(
                typeof(BufferElementDataWithMultipleWrappedValues),
                shouldContainErrors: nameof(UserError.DC0039));
        }

        [Test]
        public void BufferElementWithExplicitLayoutThrowsError()
        {
            AssertProducesError(
                typeof(BufferElementWithExplicitLayout),
                shouldContainErrors: nameof(UserError.DC0042));
        }

        protected override void AssertProducesInternal(Type systemType, DiagnosticType expectedDiagnosticType, string[] errorIdentifiers, bool useFailResolver = false)
        {
            DiagnosticMessage error = null;

            try
            {
                AuthoringComponentPostProcessor.CreateBufferElementDataAuthoringType(TypeDefinitionFor(systemType));
            }
            catch (FoundErrorInUserCodeException exception)
            {
                error = exception.DiagnosticMessages.Single();
            }

            Assert.AreEqual(expected: expectedDiagnosticType, actual: error?.DiagnosticType);
            Assert.IsTrue(error?.MessageData.Contains(errorIdentifiers.Single()));
        }
    }
}
