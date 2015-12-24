
import mtm.ioc.Container as Container
import uuid

def NoAssertion(obj):
    return True

'''
Note that we use JIT dependency resolution here so that you can have
circular dependencies
'''

class InjectBase:
    def __init__(self, identifier, assertion=NoAssertion):
        self._identifier = identifier
        self._assertion = assertion
        self._id = '__' + str(uuid.uuid4()).replace('-', '')

    def __get__(self, obj, T):
        value = getattr(obj, self._id, None)

        if not value:
            value = self._request()
            setattr(obj, self._id, value)

        return value

class Inject(InjectBase):
    def __init__(self, identifier, assertion=NoAssertion):
        InjectBase.__init__(self, identifier, assertion)

    def _request(self):
        obj = Container.resolve(self._identifier)
        self._assertion(obj)
        return obj

class InjectMany(InjectBase):
    def __init__(self, identifier, assertion=NoAssertion):
        InjectBase.__init__(self, identifier, assertion)

    def _request(self):
        objects = Container.resolveMany(self._identifier)
        for obj in objects:
            self._assertion(obj)
        return objects

class InjectOptional(InjectBase):
    def __init__(self, identifier, default, assertion=NoAssertion):
        InjectBase.__init__(self, identifier, assertion)

        self._assertion(default)
        self._default = default

    def _request(self):
        if not Container.hasBinding(self._identifier):
            return self._default

        obj = Container.resolve(self._identifier)
        self._assertion(obj)
        return obj
