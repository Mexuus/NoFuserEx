using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace NoFuserEx.Deobfuscator.Deobfuscators.Constants {
    class LongArrayDecrypter : DecrypterBase {
        readonly MethodDef decrypterMethod;
        readonly MethodDef method;
        readonly int index;

        internal LongArrayDecrypter(MethodDef method, MethodDef decrypterMethod, int index) {
            this.method = method;
            this.decrypterMethod = decrypterMethod;
            this.index = index;
        }

        internal override bool Decrypt() {
            var instructions = method.Body.Instructions;
            var valueDecrypter = GetValueDecrypter(instructions, index);
            var result = MethodInvoker<long[]>(decrypterMethod,
                (uint)valueDecrypter.GetLdcI4Value());
            if (result == null)
                return false;

            Logger.VeryVerbose($"Long array decrypted: {result.Length} longs.");

            valueDecrypter.OpCode = OpCodes.Nop;
            valueDecrypter.Operand = null;
            var decrypterInstruction = instructions[index];
            var local = CreateArray(method, index, result);
            decrypterInstruction.OpCode = OpCodes.Ldloc;
            decrypterInstruction.Operand = local;
            return true;
        }

        static Local CreateArray(MethodDef method, int index, IList<long> array) {
            var corLib = method.Module.ImportAsTypeSig(typeof(long[]));
            var local = method.Body.Variables.Add(new Local(corLib));
            var instructions = method.Body.Instructions;

            var list = new List<Instruction> {
                Instruction.CreateLdcI4(array.Count),
                Instruction.Create(OpCodes.Newarr, method.Module.CorLibTypes.Int64),
                Instruction.Create(OpCodes.Stloc, local)
            };
            for (var i = 0; i < array.Count; i++) {
                list.Add(Instruction.Create(OpCodes.Ldloc, local));
                list.Add(Instruction.CreateLdcI4(i));
                list.Add(Instruction.Create(OpCodes.Ldc_I8, array[i]));
                list.Add(Instruction.Create(OpCodes.Stelem_I8));
            }

            for (var i = 0; i < list.Count; i++) {
                instructions.Insert(index + i, list[i]);
            }
            return local;
        }
    }
}
