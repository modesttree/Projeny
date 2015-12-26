
class Assertion(Exception):
    pass

def triggerAssert(message, *params):
    if not message:
        raise Assertion('Assert hit!')

    if len(params) > 0:
        message = message.format(*params)
    raise Assertion(message)

def _triggerAssertWithMessage(extraMessage, message, *params):
    fullMessage = ''
    if message:
        fullMessage += message + ": "

    fullMessage += extraMessage
    triggerAssert(fullMessage, *params)

def assertIsNone(value, message = None, *params):
    if value != None:
        triggerAssert(message, *params)

def assertIsNotNone(value, message = None, *params):
    if value == None:
        triggerAssert(message, *params)

def assertThat(value, message = None, *params):
    if not value:
        triggerAssert(message, *params)

def assertIsEqual(left, right, message = None, *params):
    if left != right:
        _triggerAssertWithMessage(
            "Expected {0} but found {1}".format(right, left), message, *params)

def assertIsNotEqual(left, right, message = None, *params):
    if left == right:
        _triggerAssertWithMessage(
            "Expected {0} to differ from {1}".format(right, left), message, *params)

def assertIsType(value, expectedType, message = None, *params):
    if type(value) != expectedType:
        _triggerAssertWithMessage(
            "Expected type '{0}' but found '{1}'".format(expectedType.__name__, type(value).__name__), message, *params)

def assertRaises(exceptionType, handler, message = None, *params):
    try:
        handler()
    except Exception as e:
        exc = e

    if not exc:
        _triggerAssertWithMessage(
            "Expected exception {0} to be raised, but none was", exceptionType.__name__, message, *params)

    if isinstance(exc, exceptionType):
        return

    _triggerAssertWithMessage(
        "Expected exception {0} to be raised, but instead got exception {1}".format(exceptionType.__name__, type(exc).__name__),
        message, *params)

def assertRaisesAny(handler, message = None, *params):
    try:
        handler()
    except:
        return

    _triggerAssertWithMessage("Expected exception to be raised, but none was", message, *params)

if __name__ == '__main__':

    #assertThat(False, "lorem {0} ipsum", 5)
    #assertIsNotEqual(1, 1)

    #assertRaisesAny(lambda: ""[0])
    assertRaises(Exception, lambda: ""[0])
    assertRaisesAny(lambda: ""[0])

    class TempExc(Exception):
        pass

    def testThrow():
        raise TempExc()

    assertRaises(TempExc, lambda: testThrow())

    #assertIsType("sdf", str)
    #assertIsEqual(5, 1)
    #assertIsEqual(5, 1, "foo")
    print("succeeded")
