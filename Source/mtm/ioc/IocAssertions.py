
from mtm.util.Assert import *
import collections

def IsInstanceOf(*classes):
   def test(obj):
       assertThat(obj is None or isinstance(obj, classes), \
           "Expected one of types '{0}' but found '{1}'".format(', '.join(map(lambda t: t.__name__, classes)), type(obj).__name__))
   return test

def HasAttributes(*attributes):
   def test(obj):
      for each in attributes:
         if not hasattr(obj, each): return False
      return True
   return test

def HasMethods(*methods):
   def test(obj):
      for methodName in methods:
         assertThat(hasattr(obj, methodName), \
             "Unable to find method '{0}' on object with type '{1}'".format(methodName, type(obj).__name__))
         assertThat(isinstance(getattr(obj, methodName), collections.Callable))
      return True
   return test

