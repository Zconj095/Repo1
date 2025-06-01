 
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\MonoBleedingEdge\bin\mono-gdb.py---------------


#
# Author: Zoltan Varga (vargaz@gmail.com)
# License: MIT/X11
#

#
# This is a mono support mode for gdb 7.0 and later
# Usage:
# - copy/symlink this file to the directory where the mono executable lives.
# - run mono under gdb, or attach to a mono process started with --debug=gdb using gdb.
#

from __future__ import print_function
import os

class StringPrinter:
    "Print a C# string"

    def __init__(self, val):
        self.val = val

    def to_string(self):
        if int(self.val.cast (gdb.lookup_type ("guint64"))) == 0:
            return "null"

        obj = self.val.cast (gdb.lookup_type ("MonoString").pointer ()).dereference ()
        len = obj ['length']
        chars = obj ['chars']
        i = 0
        res = ['"']
        while i < len:
            val = (chars.cast(gdb.lookup_type ("gint64")) + (i * 2)).cast(gdb.lookup_type ("gunichar2").pointer ()).dereference ()
            if val >= 256:
                c = unichr (val)
            else:
                c = chr (val)
            res.append (c)
            i = i + 1
        res.append ('"')
        return ''.join (res)

def stringify_class_name(ns, name):
    if ns == "System":
        if name == "Byte":
            return "byte"
        if name == "String":
            return "string"
    if ns == "":
        return name
    else:
        return "{0}.{1}".format (ns, name)

class ArrayPrinter:
    "Print a C# array"

    def __init__(self, val, class_ns, class_name):
        self.val = val
        self.class_ns = class_ns
        self.class_name = class_name

    def to_string(self):
        obj = self.val.cast (gdb.lookup_type ("MonoArray").pointer ()).dereference ()
        length = obj ['max_length']
        return "{0} [{1}]".format (stringify_class_name (self.class_ns, self.class_name [0:len(self.class_name) - 2]), int(length))
        
class ObjectPrinter:
    "Print a C# object"

    def __init__(self, val):
        if str(val.type)[-1] == "&":
            self.val = val.address.cast (gdb.lookup_type ("MonoObject").pointer ())
        else:
            self.val = val.cast (gdb.lookup_type ("MonoObject").pointer ())

    class _iterator:
        def __init__(self,obj):
            self.obj = obj
            self.iter = self.obj.type.fields ().__iter__ ()
            pass

        def __iter__(self):
            return self

        def next(self):
            field = self.iter.next ()
            try:
                if str(self.obj [field.name].type) == "object":
                    # Avoid recursion
                    return (field.name, self.obj [field.name].cast (gdb.lookup_type ("void").pointer ()))
                else:
                    return (field.name, self.obj [field.name])
            except:
                # Superclass
                return (field.name, self.obj.cast (gdb.lookup_type ("{0}".format (field.name))))

    def children(self):
        # FIXME: It would be easier if gdb.Value would support iteration itself
        # It would also be better if we could return None
        if int(self.val.cast (gdb.lookup_type ("guint64"))) == 0:
            return {}.__iter__ ()
        try:
            obj = self.val.dereference ()
            class_ns = obj ['vtable'].dereference ()['klass'].dereference ()['name_space'].string ()
            class_name = obj ['vtable'].dereference ()['klass'].dereference ()['name'].string ()
            if class_name [-2:len(class_name)] == "[]":
                return {}.__iter__ ()
            try:
                gdb_type = gdb.lookup_type ("struct {0}_{1}".format (class_ns.replace (".", "_"), class_name))
                return self._iterator(obj.cast (gdb_type))
            except:
                return {}.__iter__ ()
        except:
            print (sys.exc_info ()[0])
            print (sys.exc_info ()[1])
            return {}.__iter__ ()

    def to_string(self):
        if int(self.val.cast (gdb.lookup_type ("guint64"))) == 0:
            return "null"
        try:
            obj = self.val.dereference ()
            class_ns = obj ['vtable'].dereference ()['klass'].dereference ()['name_space'].string ()
            class_name = obj ['vtable'].dereference ()['klass'].dereference ()['name'].string ()
            if class_ns == "System" and class_name == "String":
                return StringPrinter (self.val).to_string ()
            if class_name [-2:len(class_name)] == "[]":
                return ArrayPrinter (self.val,class_ns,class_name).to_string ()
            if class_ns != "":
                try:
                    gdb_type = gdb.lookup_type ("struct {0}.{1}".format (class_ns, class_name))
                except:
                    # Maybe there is no debug info for that type
                    return "{0}.{1}".format (class_ns, class_name)
                #return obj.cast (gdb_type)
                return "{0}.{1}".format (class_ns, class_name)
            return class_name
        except:
            print (sys.exc_info ()[0])
            print (sys.exc_info ()[1])
            # FIXME: This can happen because we don't have liveness information
            return self.val.cast (gdb.lookup_type ("guint64"))
        
class MonoMethodPrinter:
    "Print a MonoMethod structure"

    def __init__(self, val):
        self.val = val

    def to_string(self):
        if int(self.val.cast (gdb.lookup_type ("guint64"))) == 0:
            return "0x0"
        val = self.val.dereference ()
        klass = val ["klass"].dereference ()
        class_name = stringify_class_name (klass ["name_space"].string (), klass ["name"].string ())
        return "\"{0}:{1} ()\"".format (class_name, val ["name"].string ())
        # This returns more info but requires calling into the inferior
        #return "\"{0}\"".format (gdb.parse_and_eval ("mono_method_full_name ({0}, 1)".format (str (int (self.val.cast (gdb.lookup_type ("guint64")))))).string ())

class MonoClassPrinter:
    "Print a MonoClass structure"

    def __init__(self, val):
        self.val = val

    def to_string_inner(self, add_quotes):
        if int(self.val.cast (gdb.lookup_type ("guint64"))) == 0:
            return "0x0"
        klass = self.val.dereference ()
        class_name = stringify_class_name (klass ["name_space"].string (), klass ["name"].string ())
        if add_quotes:
            return "\"{0}\"".format (class_name)
        else:
            return class_name
        # This returns more info but requires calling into the inferior
        #return "\"{0}\"".format (gdb.parse_and_eval ("mono_type_full_name (&((MonoClass*){0})->byval_arg)".format (str (int ((self.val).cast (gdb.lookup_type ("guint64")))))))

    def to_string(self):
        try:
            return self.to_string_inner (True)
        except:
            #print (sys.exc_info ()[0])
            #print (sys.exc_info ()[1])
            return str(self.val.cast (gdb.lookup_type ("gpointer")))

class MonoGenericInstPrinter:
    "Print a MonoGenericInst structure"

    def __init__(self, val):
        self.val = val

    def to_string(self):
        if int(self.val.cast (gdb.lookup_type ("guint64"))) == 0:
            return "0x0"
        inst = self.val.dereference ()
        inst_len = inst ["type_argc"]
        inst_args = inst ["type_argv"]
        inst_str = ""
        for i in range(0, inst_len):
            # print (inst_args)
            type_printer = MonoTypePrinter (inst_args [i])
            if i > 0:
                inst_str = inst_str + ", "
            inst_str = inst_str + type_printer.to_string ()
        return inst_str

class MonoGenericClassPrinter:
    "Print a MonoGenericClass structure"

    def __init__(self, val):
        self.val = val

    def to_string_inner(self):
        gclass = self.val.dereference ()
        container_str = str(gclass ["container_class"])
        class_inst = gclass ["context"]["class_inst"]
        class_inst_str = ""
        if int(class_inst.cast (gdb.lookup_type ("guint64"))) != 0:
            class_inst_str  = str(class_inst)
        method_inst = gclass ["context"]["method_inst"]
        method_inst_str = ""
        if int(method_inst.cast (gdb.lookup_type ("guint64"))) != 0:
            method_inst_str  = str(method_inst)
        return "{0}, [{1}], [{2}]>".format (container_str, class_inst_str, method_inst_str)

    def to_string(self):
        try:
            return self.to_string_inner ()
        except:
            #print (sys.exc_info ()[0])
            #print (sys.exc_info ()[1])
            return str(self.val.cast (gdb.lookup_type ("gpointer")))

class MonoTypePrinter:
    "Print a MonoType structure"

    def __init__(self, val):
        self.val = val

    def to_string_inner(self, csharp):
        try:
            t = self.val.referenced_value ()

            kind = str (t ["type"]).replace ("MONO_TYPE_", "").lower ()
            info = ""

            if kind == "class":
                p = MonoClassPrinter(t ["data"]["klass"])
                info = p.to_string ()
            elif kind == "genericinst":
                info = str(t ["data"]["generic_class"])

            if info != "":
                return "{{{0}, {1}}}".format (kind, info)
            else:
                return "{{{0}}}".format (kind)
        except:
            #print (sys.exc_info ()[0])
            #print (sys.exc_info ()[1])
            return str(self.val.cast (gdb.lookup_type ("gpointer")))

    def to_string(self):
        return self.to_string_inner (False)

class MonoMethodRgctxPrinter:
    "Print a MonoMethodRgctx structure"

    def __init__(self, val):
        self.val = val

    def to_string(self):
        rgctx = self.val.dereference ()
        klass = rgctx ["class_vtable"].dereference () ["klass"]
        klass_printer = MonoClassPrinter (klass)
        inst = rgctx ["method_inst"].dereference ()
        inst_len = inst ["type_argc"]
        inst_args = inst ["type_argv"]
        inst_str = ""
        for i in range(0, inst_len):
            # print (inst_args)
            type_printer = MonoTypePrinter (inst_args [i])
            if i > 0:
                inst_str = inst_str + ", "
            inst_str = inst_str + type_printer.to_string ()
        return "MRGCTX[{0}, [{1}]]".format (klass_printer.to_string(), inst_str)

class MonoVTablePrinter:
    "Print a MonoVTable structure"

    def __init__(self, val):
        self.val = val

    def to_string(self):
        if int(self.val.cast (gdb.lookup_type ("guint64"))) == 0:
            return "0x0"
        vtable = self.val.dereference ()
        klass = vtable ["klass"]
        klass_printer = MonoClassPrinter (klass)

        return "vtable({0})".format (klass_printer.to_string ())

def lookup_pretty_printer(val):
    t = str (val.type)
    if t == "object":
        return ObjectPrinter (val)
    if t[0:5] == "class" and t[-1] == "&":
        return ObjectPrinter (val)    
    if t == "string":
        return StringPrinter (val)
    if t == "MonoString *":
        return StringPrinter (val)
    if t == "MonoMethod *":
        return MonoMethodPrinter (val)
    if t == "MonoClass *":
        return MonoClassPrinter (val)
    if t == "MonoType *":
        return MonoTypePrinter (val)
    if t == "MonoGenericInst *":
        return MonoGenericInstPrinter (val)
    if t == "MonoGenericClass *":
        return MonoGenericClassPrinter (val)
    if t == "MonoMethodRuntimeGenericContext *":
        return MonoMethodRgctxPrinter (val)
    if t == "MonoVTable *":
        return MonoVTablePrinter (val)
    return None

def register_csharp_printers(obj):
    "Register C# pretty-printers with objfile Obj."

    if obj == None:
        obj = gdb

    obj.pretty_printers.append (lookup_pretty_printer)

# This command will flush the debugging info collected by the runtime
class XdbCommand (gdb.Command):
    def __init__ (self):
        super (XdbCommand, self).__init__ ("xdb", gdb.COMMAND_NONE,
                                           gdb.COMPLETE_COMMAND)

    def invoke(self, arg, from_tty):
        gdb.execute ("call mono_xdebug_flush ()")

register_csharp_printers (gdb.current_objfile())

XdbCommand ()

gdb.execute ("set environment MONO_XDEBUG gdb")

print ("Mono support loaded.")




#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\MonoBleedingEdge\bin\mono-gdb.py---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\MonoBleedingEdge\bin\mono-sgen-gdb.py---------------


#
# Author: Zoltan Varga (vargaz@gmail.com)
# License: MIT/X11
#

#
# This is a mono support mode for gdb 7.0 and later
# Usage:
# - copy/symlink this file to the directory where the mono executable lives.
# - run mono under gdb, or attach to a mono process started with --debug=gdb using gdb.
#

from __future__ import print_function
import os

class StringPrinter:
    "Print a C# string"

    def __init__(self, val):
        self.val = val

    def to_string(self):
        if int(self.val.cast (gdb.lookup_type ("guint64"))) == 0:
            return "null"

        obj = self.val.cast (gdb.lookup_type ("MonoString").pointer ()).dereference ()
        len = obj ['length']
        chars = obj ['chars']
        i = 0
        res = ['"']
        while i < len:
            val = (chars.cast(gdb.lookup_type ("gint64")) + (i * 2)).cast(gdb.lookup_type ("gunichar2").pointer ()).dereference ()
            if val >= 256:
                c = unichr (val)
            else:
                c = chr (val)
            res.append (c)
            i = i + 1
        res.append ('"')
        return ''.join (res)

def stringify_class_name(ns, name):
    if ns == "System":
        if name == "Byte":
            return "byte"
        if name == "String":
            return "string"
    if ns == "":
        return name
    else:
        return "{0}.{1}".format (ns, name)

class ArrayPrinter:
    "Print a C# array"

    def __init__(self, val, class_ns, class_name):
        self.val = val
        self.class_ns = class_ns
        self.class_name = class_name

    def to_string(self):
        obj = self.val.cast (gdb.lookup_type ("MonoArray").pointer ()).dereference ()
        length = obj ['max_length']
        return "{0} [{1}]".format (stringify_class_name (self.class_ns, self.class_name [0:len(self.class_name) - 2]), int(length))
        
class ObjectPrinter:
    "Print a C# object"

    def __init__(self, val):
        if str(val.type)[-1] == "&":
            self.val = val.address.cast (gdb.lookup_type ("MonoObject").pointer ())
        else:
            self.val = val.cast (gdb.lookup_type ("MonoObject").pointer ())

    class _iterator:
        def __init__(self,obj):
            self.obj = obj
            self.iter = self.obj.type.fields ().__iter__ ()
            pass

        def __iter__(self):
            return self

        def next(self):
            field = self.iter.next ()
            try:
                if str(self.obj [field.name].type) == "object":
                    # Avoid recursion
                    return (field.name, self.obj [field.name].cast (gdb.lookup_type ("void").pointer ()))
                else:
                    return (field.name, self.obj [field.name])
            except:
                # Superclass
                return (field.name, self.obj.cast (gdb.lookup_type ("{0}".format (field.name))))

    def children(self):
        # FIXME: It would be easier if gdb.Value would support iteration itself
        # It would also be better if we could return None
        if int(self.val.cast (gdb.lookup_type ("guint64"))) == 0:
            return {}.__iter__ ()
        try:
            obj = self.val.dereference ()
            class_ns = obj ['vtable'].dereference ()['klass'].dereference ()['name_space'].string ()
            class_name = obj ['vtable'].dereference ()['klass'].dereference ()['name'].string ()
            if class_name [-2:len(class_name)] == "[]":
                return {}.__iter__ ()
            try:
                gdb_type = gdb.lookup_type ("struct {0}_{1}".format (class_ns.replace (".", "_"), class_name))
                return self._iterator(obj.cast (gdb_type))
            except:
                return {}.__iter__ ()
        except:
            print (sys.exc_info ()[0])
            print (sys.exc_info ()[1])
            return {}.__iter__ ()

    def to_string(self):
        if int(self.val.cast (gdb.lookup_type ("guint64"))) == 0:
            return "null"
        try:
            obj = self.val.dereference ()
            class_ns = obj ['vtable'].dereference ()['klass'].dereference ()['name_space'].string ()
            class_name = obj ['vtable'].dereference ()['klass'].dereference ()['name'].string ()
            if class_ns == "System" and class_name == "String":
                return StringPrinter (self.val).to_string ()
            if class_name [-2:len(class_name)] == "[]":
                return ArrayPrinter (self.val,class_ns,class_name).to_string ()
            if class_ns != "":
                try:
                    gdb_type = gdb.lookup_type ("struct {0}.{1}".format (class_ns, class_name))
                except:
                    # Maybe there is no debug info for that type
                    return "{0}.{1}".format (class_ns, class_name)
                #return obj.cast (gdb_type)
                return "{0}.{1}".format (class_ns, class_name)
            return class_name
        except:
            print (sys.exc_info ()[0])
            print (sys.exc_info ()[1])
            # FIXME: This can happen because we don't have liveness information
            return self.val.cast (gdb.lookup_type ("guint64"))
        
class MonoMethodPrinter:
    "Print a MonoMethod structure"

    def __init__(self, val):
        self.val = val

    def to_string(self):
        if int(self.val.cast (gdb.lookup_type ("guint64"))) == 0:
            return "0x0"
        val = self.val.dereference ()
        klass = val ["klass"].dereference ()
        class_name = stringify_class_name (klass ["name_space"].string (), klass ["name"].string ())
        return "\"{0}:{1} ()\"".format (class_name, val ["name"].string ())
        # This returns more info but requires calling into the inferior
        #return "\"{0}\"".format (gdb.parse_and_eval ("mono_method_full_name ({0}, 1)".format (str (int (self.val.cast (gdb.lookup_type ("guint64")))))).string ())

class MonoClassPrinter:
    "Print a MonoClass structure"

    def __init__(self, val):
        self.val = val

    def to_string_inner(self, add_quotes):
        if int(self.val.cast (gdb.lookup_type ("guint64"))) == 0:
            return "0x0"
        klass = self.val.dereference ()
        class_name = stringify_class_name (klass ["name_space"].string (), klass ["name"].string ())
        if add_quotes:
            return "\"{0}\"".format (class_name)
        else:
            return class_name
        # This returns more info but requires calling into the inferior
        #return "\"{0}\"".format (gdb.parse_and_eval ("mono_type_full_name (&((MonoClass*){0})->byval_arg)".format (str (int ((self.val).cast (gdb.lookup_type ("guint64")))))))

    def to_string(self):
        try:
            return self.to_string_inner (True)
        except:
            #print (sys.exc_info ()[0])
            #print (sys.exc_info ()[1])
            return str(self.val.cast (gdb.lookup_type ("gpointer")))

class MonoGenericInstPrinter:
    "Print a MonoGenericInst structure"

    def __init__(self, val):
        self.val = val

    def to_string(self):
        if int(self.val.cast (gdb.lookup_type ("guint64"))) == 0:
            return "0x0"
        inst = self.val.dereference ()
        inst_len = inst ["type_argc"]
        inst_args = inst ["type_argv"]
        inst_str = ""
        for i in range(0, inst_len):
            # print (inst_args)
            type_printer = MonoTypePrinter (inst_args [i])
            if i > 0:
                inst_str = inst_str + ", "
            inst_str = inst_str + type_printer.to_string ()
        return inst_str

class MonoGenericClassPrinter:
    "Print a MonoGenericClass structure"

    def __init__(self, val):
        self.val = val

    def to_string_inner(self):
        gclass = self.val.dereference ()
        container_str = str(gclass ["container_class"])
        class_inst = gclass ["context"]["class_inst"]
        class_inst_str = ""
        if int(class_inst.cast (gdb.lookup_type ("guint64"))) != 0:
            class_inst_str  = str(class_inst)
        method_inst = gclass ["context"]["method_inst"]
        method_inst_str = ""
        if int(method_inst.cast (gdb.lookup_type ("guint64"))) != 0:
            method_inst_str  = str(method_inst)
        return "{0}, [{1}], [{2}]>".format (container_str, class_inst_str, method_inst_str)

    def to_string(self):
        try:
            return self.to_string_inner ()
        except:
            #print (sys.exc_info ()[0])
            #print (sys.exc_info ()[1])
            return str(self.val.cast (gdb.lookup_type ("gpointer")))

class MonoTypePrinter:
    "Print a MonoType structure"

    def __init__(self, val):
        self.val = val

    def to_string_inner(self, csharp):
        try:
            t = self.val.referenced_value ()

            kind = str (t ["type"]).replace ("MONO_TYPE_", "").lower ()
            info = ""

            if kind == "class":
                p = MonoClassPrinter(t ["data"]["klass"])
                info = p.to_string ()
            elif kind == "genericinst":
                info = str(t ["data"]["generic_class"])

            if info != "":
                return "{{{0}, {1}}}".format (kind, info)
            else:
                return "{{{0}}}".format (kind)
        except:
            #print (sys.exc_info ()[0])
            #print (sys.exc_info ()[1])
            return str(self.val.cast (gdb.lookup_type ("gpointer")))

    def to_string(self):
        return self.to_string_inner (False)

class MonoMethodRgctxPrinter:
    "Print a MonoMethodRgctx structure"

    def __init__(self, val):
        self.val = val

    def to_string(self):
        rgctx = self.val.dereference ()
        klass = rgctx ["class_vtable"].dereference () ["klass"]
        klass_printer = MonoClassPrinter (klass)
        inst = rgctx ["method_inst"].dereference ()
        inst_len = inst ["type_argc"]
        inst_args = inst ["type_argv"]
        inst_str = ""
        for i in range(0, inst_len):
            # print (inst_args)
            type_printer = MonoTypePrinter (inst_args [i])
            if i > 0:
                inst_str = inst_str + ", "
            inst_str = inst_str + type_printer.to_string ()
        return "MRGCTX[{0}, [{1}]]".format (klass_printer.to_string(), inst_str)

class MonoVTablePrinter:
    "Print a MonoVTable structure"

    def __init__(self, val):
        self.val = val

    def to_string(self):
        if int(self.val.cast (gdb.lookup_type ("guint64"))) == 0:
            return "0x0"
        vtable = self.val.dereference ()
        klass = vtable ["klass"]
        klass_printer = MonoClassPrinter (klass)

        return "vtable({0})".format (klass_printer.to_string ())

def lookup_pretty_printer(val):
    t = str (val.type)
    if t == "object":
        return ObjectPrinter (val)
    if t[0:5] == "class" and t[-1] == "&":
        return ObjectPrinter (val)    
    if t == "string":
        return StringPrinter (val)
    if t == "MonoString *":
        return StringPrinter (val)
    if t == "MonoMethod *":
        return MonoMethodPrinter (val)
    if t == "MonoClass *":
        return MonoClassPrinter (val)
    if t == "MonoType *":
        return MonoTypePrinter (val)
    if t == "MonoGenericInst *":
        return MonoGenericInstPrinter (val)
    if t == "MonoGenericClass *":
        return MonoGenericClassPrinter (val)
    if t == "MonoMethodRuntimeGenericContext *":
        return MonoMethodRgctxPrinter (val)
    if t == "MonoVTable *":
        return MonoVTablePrinter (val)
    return None

def register_csharp_printers(obj):
    "Register C# pretty-printers with objfile Obj."

    if obj == None:
        obj = gdb

    obj.pretty_printers.append (lookup_pretty_printer)

# This command will flush the debugging info collected by the runtime
class XdbCommand (gdb.Command):
    def __init__ (self):
        super (XdbCommand, self).__init__ ("xdb", gdb.COMMAND_NONE,
                                           gdb.COMPLETE_COMMAND)

    def invoke(self, arg, from_tty):
        gdb.execute ("call mono_xdebug_flush ()")

register_csharp_printers (gdb.current_objfile())

XdbCommand ()

gdb.execute ("set environment MONO_XDEBUG gdb")

print ("Mono support loaded.")




#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\MonoBleedingEdge\bin\mono-sgen-gdb.py---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\MonoBleedingEdge\lib\mono\lldb\mono.py---------------
.
.
#
# Author: Zoltan Varga (vargaz@gmail.com)
# License: MIT/X11
#

#
# This is a mono support mode for lldb
#

# Comments about the lldb python api:
# - there are no accessors, i.e. valobj["name"]
# - http://lldb.llvm.org/python_reference/index.html seems to be outdated
# - there is no autoload support, i.e. can't load this file automatically
#   when 'mono' is the debugger target.

import lldb

# FIXME: Generate enums from runtime enums
MONO_TYPE_END        = 0x00
MONO_TYPE_VOID       = 0x01
MONO_TYPE_BOOLEAN    = 0x02
MONO_TYPE_CHAR       = 0x03
MONO_TYPE_I1         = 0x04
MONO_TYPE_U1         = 0x05
MONO_TYPE_I2         = 0x06
MONO_TYPE_U2         = 0x07
MONO_TYPE_I4         = 0x08
MONO_TYPE_U4         = 0x09
MONO_TYPE_I8         = 0x0a
MONO_TYPE_U8         = 0x0b
MONO_TYPE_R4         = 0x0c
MONO_TYPE_R8         = 0x0d
MONO_TYPE_STRING     = 0x0e
MONO_TYPE_PTR        = 0x0f
MONO_TYPE_BYREF      = 0x10
MONO_TYPE_VALUETYPE  = 0x11
MONO_TYPE_CLASS      = 0x12
MONO_TYPE_VAR	     = 0x13
MONO_TYPE_ARRAY      = 0x14
MONO_TYPE_GENERICINST= 0x15
MONO_TYPE_TYPEDBYREF = 0x16
MONO_TYPE_I          = 0x18
MONO_TYPE_U          = 0x19
MONO_TYPE_FNPTR      = 0x1b
MONO_TYPE_OBJECT     = 0x1c
MONO_TYPE_SZARRAY    = 0x1d
MONO_TYPE_MVAR	     = 0x1e

primitive_type_names = {
    MONO_TYPE_BOOLEAN : "bool",
    MONO_TYPE_CHAR : "char",
    MONO_TYPE_I1 : "sbyte",
    MONO_TYPE_U1 : "byte",
    MONO_TYPE_I2 : "short",
    MONO_TYPE_U2 : "ushort",
    MONO_TYPE_I4 : "int",
    MONO_TYPE_U4 : "uint",
    MONO_TYPE_I8 : "long",
    MONO_TYPE_U8 : "ulong",
    MONO_TYPE_R4 : "float",
    MONO_TYPE_R8 : "double",
    MONO_TYPE_STRING : "string"
    }

#
# Helper functions for working with the lldb python api
#

def member(val, member_name):
    return val.GetChildMemberWithName (member_name)

def string_member(val, member_name):
    return val.GetChildMemberWithName (member_name).GetSummary ()[1:-1]

def isnull(val):
    return val.deref.addr.GetOffset () == 0

def stringify_class_name(ns, name):
    if ns == "System":
        if name == "Byte":
            return "byte"
        if name == "String":
            return "string"
    if ns == "":
        return name
    else:
        return "{0}.{1}".format (ns, name)

#
# Pretty printers for mono runtime types
#

def stringify_type (type):
    "Print a MonoType structure"
    ttype = member(type, "type").GetValueAsUnsigned()
    if primitive_type_names.has_key (ttype):
        return primitive_type_names [ttype]
    else:
        return "<MonoTypeEnum 0x{0:x}>".format (ttype)

def stringify_ginst (ginst):
    "Print a MonoGenericInst structure"
    len = int(member(ginst, "type_argc").GetValue())
    argv = member(ginst, "type_argv")
    res=""
    for i in range(len):
        t = argv.GetChildAtIndex(i, False, True)
        if i > 0:
            res += ", "
        res += stringify_type(t)
    return res

def print_type(valobj, internal_dict):
    type = valobj
    if isnull (type):
        return ""
    return stringify_type (type)

def print_class (valobj, internal_dict):
    klass = valobj
    if isnull (klass):
        return ""
    aname = member (member (member (klass, "image"), "assembly"), "aname")
    basename = "[{0}]{1}".format (string_member (aname, "name"), (stringify_class_name (string_member (klass, "name_space"), string_member (klass, "name"))))
    gclass = member (klass, "generic_class")
    if not isnull (gclass):
        ginst = member (member (gclass, "context"), "class_inst")
        return "{0}<{1}>".format (basename, stringify_ginst (ginst))
    return basename

def print_method (valobj, internal_dict):
    method = valobj
    if isnull (method):
        return ""
    klass = member (method, "klass")
    return "{0}:{1}()".format (print_class (klass, None), string_member (valobj, "name"))

def print_domain(valobj, internal_dict):
    domain = valobj
    if isnull (domain):
        return ""
    target = domain.target
    root = target.FindFirstGlobalVariable("mono_root_domain")
    name = string_member (domain, "friendly_name")
    if root.IsValid () and root.deref.addr.GetOffset () == root.deref.addr.GetOffset ():
        return "[root]"
    else:
        return "[{0}]".format (name)

def print_object(valobj, internal_dict):
    obj = valobj
    if isnull (obj):
        return ""
    domain = member (member (obj, "vtable"), "domain")
    klass = member (member (obj, "vtable"), "klass")
    return print_domain (domain, None) + print_class (klass, None)

# Register pretty printers
# FIXME: This cannot pick up the methods define in this module, leading to warnings
lldb.debugger.HandleCommand ("type summary add -w mono -F mono.print_method MonoMethod")
lldb.debugger.HandleCommand ("type summary add -w mono -F mono.print_class MonoClass")
lldb.debugger.HandleCommand ("type summary add -w mono -F mono.print_type MonoType")
lldb.debugger.HandleCommand ("type summary add -w mono -F mono.print_domain MonoDomain")
lldb.debugger.HandleCommand ("type summary add -w mono -F mono.print_object MonoObject")
lldb.debugger.HandleCommand ("type category enable mono")

# Helper commands for runtime debugging
# These resume the target
# Print the method at the current ip
lldb.debugger.HandleCommand ("command alias pip p mono_print_method_from_ip((void*)$pc)")
# Print the method at the provided ip
lldb.debugger.HandleCommand ("command regex pmip 's/^$/p mono_print_method_from_ip((void*)$pc)/' 's/(.+)/p mono_print_method_from_ip((void*)(%1))/'")

print "Mono support mode loaded."
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\MonoBleedingEdge\lib\mono\lldb\mono.py---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\ModoToUnity.py---------------
.
.
#!/usr/bin/env python

################################################################################
#
# convert.py
#
# Version: 1.000
#
# Author: Gwynne Reddick
#
# Description:
#
#
# Usage: @convert.py {infile} {outfile} {format} {logfile}
#
# Arguments: infile     -   path and filename of input file
#            outfile    -   path and filename of output file
#            format     -   output file format. for FBX this is either 'FBX' or ' FBX 2006.11'
#            logfile    -   path and filename of output lig (optional)
#
# Examples: @convert.py {L:\Luxology\fbxtest.lxo} {L:\Luxology\FBXout.fbx} {FBX} {L:\Luxology\logout.txt}
#           @convert.py {L:\Luxology\fbxtest.lxo} {L:\Luxology\FBXout.fbx} {FBX 2006.11} {}
#
#
# Last Update 12:03 09/04/10
#
################################################################################

try:
    lx.eval('log.toConsole true')
    argstring = lx.arg()
    args = argstring.split('} ')
    for index, arg in enumerate(args):
        args[index] = arg.strip('{}')
    try:
        infile, outfile, logfile = args
    except:
        raise Exception('Wrong number of arguments provided')
    format = 'FBX'
    extension = 'fbx'
    # set useCollada to 0 if you want to use FBX as intermediate format
    useCollada = 1
    if useCollada == 1:
        format = 'COLLADA_141'
        extension = 'dae'
    try:
        lx.eval('scene.open {%s} normal' % infile)
        lx.eval('!!scene.saveAs {%s} {%s} false' % (outfile + '.' + extension, format))
    except:
        lx.out('Exception "%s" on line: %d' % (sys.exc_value, sys.exc_traceback.tb_lineno))
    if logfile:
        lx.eval('log.masterSave {%s}' % logfile)
except:
    lx.out('Exception "%s" on line: %d' % (sys.exc_value, sys.exc_traceback.tb_lineno))
lx.eval('app.quit')
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\ModoToUnity.py---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Unity-BlenderToFBX.py---------------
.
.
import bpy
blender249 = True
blender280 = (2,80,0) <= bpy.app.version

try:
    import Blender
except:
    blender249 = False

if not blender280:
    if blender249:
        try:
            import export_fbx
        except:
            print('error: export_fbx not found.')
            Blender.Quit()
    else :
        try:
            import io_scene_fbx.export_fbx
        except:
            print('error: io_scene_fbx.export_fbx not found.')
            # This might need to be bpy.Quit()
            raise

# Find the Blender output file
import sys
argv = sys.argv
outfile = ' '.join(argv[argv.index("--") + 1:])

# Do the conversion
print("Starting blender to FBX conversion " + outfile)

if blender280:
    import bpy.ops
    bpy.ops.export_scene.fbx(filepath=outfile,
        check_existing=False,
        use_selection=False,
        use_active_collection=False,
        object_types= {'ARMATURE','CAMERA','LIGHT','MESH','OTHER','EMPTY'},
        use_mesh_modifiers=True,
        mesh_smooth_type='OFF',
        use_custom_props=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=False,
        apply_scale_options='FBX_SCALE_ALL')
elif blender249:
    mtx4_x90n = Blender.Mathutils.RotationMatrix(-90, 4, 'x')
    export_fbx.write(outfile,
        EXP_OBS_SELECTED=False,
        EXP_MESH=True,
        EXP_MESH_APPLY_MOD=True,
        EXP_MESH_HQ_NORMALS=True,
        EXP_ARMATURE=True,
        EXP_LAMP=True,
        EXP_CAMERA=True,
        EXP_EMPTY=True,
        EXP_IMAGE_COPY=False,
        ANIM_ENABLE=True,
        ANIM_OPTIMIZE=False,
        ANIM_ACTION_ALL=True,
        GLOBAL_MATRIX=mtx4_x90n)
else:
    # blender 2.58 or newer
    import math
    from mathutils import Matrix
    # -90 degrees
    mtx4_x90n = Matrix.Rotation(-math.pi / 2.0, 4, 'X')

    class FakeOp:
        def report(self, tp, msg):
            print("%s: %s" % (tp, msg))

    exportObjects = ['ARMATURE', 'EMPTY', 'MESH']

    minorVersion = bpy.app.version[1];
    if minorVersion <= 58:
        # 2.58
        io_scene_fbx.export_fbx.save(FakeOp(), bpy.context, filepath=outfile,
            global_matrix=mtx4_x90n,
            use_selection=False,
            object_types=exportObjects,
            mesh_apply_modifiers=True,
            ANIM_ENABLE=True,
            ANIM_OPTIMIZE=False,
            ANIM_OPTIMIZE_PRECISSION=6,
            ANIM_ACTION_ALL=True,
            batch_mode='OFF',
            BATCH_OWN_DIR=False)
    else:
        # 2.59 and later
        kwargs = io_scene_fbx.export_fbx.defaults_unity3d()
        io_scene_fbx.export_fbx.save(FakeOp(), bpy.context, filepath=outfile, **kwargs)
    # HQ normals are not supported in the current exporter

print("Finished blender to FBX conversion " + outfile)
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Unity-BlenderToFBX.py---------------
.
.
