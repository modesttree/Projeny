
import unittest

import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject
import mtm.ioc.IocAssertions as Assertions

class TestConditionalInjections(unittest.TestCase):
    ''' Test assertions in inject statements '''
    def setUp(self):
        Container.clear()

    @unittest.expectedFailure
    def testHasMethodFail(self):
        # does not have WriteLine()
        Container.bind('Console').toSingle(Foo)
        Test1().Run()

    def testHasMethodSuccess(self):
        Container.bind('Console').toSingle(Console1)
        Test1().Run()

    @unittest.expectedFailure
    def testIsInstanceFailure(self):
        Container.bind('Title').to(2)
        Test2().Run()

    def testIsInstanceSuccess(self):
        Container.bind('Title').to('yep')
        Test2().Run()

# Helper methods
class Test1:
    con = Inject('Console', Assertions.HasMethods('WriteLine'))

    def Run(self):
        self.con.WriteLine('lorem ipsum')

class Console1:
   def WriteLine(self, s):
      print('Console - ' + s)

class Foo:
    def __init__(self):
        pass

class Test2:
    title = Inject('Title', Assertions.IsInstanceOf(str))

    def Run(self):
        print('title: {0}'.format(self.title))

if __name__ == '__main__':
    unittest.main()
