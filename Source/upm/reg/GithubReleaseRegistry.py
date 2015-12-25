
from upm.ioc.Inject import Inject
from upm.ioc.Inject import InjectMany
import upm.ioc.IocAssertions as Assertions
from github import Github
from upm.util.Assert import *

class GithubReleaseRegistry:
    _log = Inject('Logger')

    def getName(self):
        return "Github"

    def tryInstallRelease(self, releaseName):
        g = Github()
        repo = g.get_repo('eventropy/Zentest')
        print("Found repo {0}".format(repo.name))

        for download in repo.get_downloads():
            print("Found download {0}".format(download.html_url))

        return False

if __name__ == '__main__':

    import upm.ioc.Container as Container
    from upm.log.Logger import Logger
    from upm.log.LogStreamConsole import LogStreamConsole

    Container.bind('Logger').toSingle(Logger)
    Container.bind('LogStream').toSingle(LogStreamConsole, True, True)

    reg = GithubReleaseRegistry()

    if reg.tryInstallRelease('Zenject'):
        print("yeah")
    else:
        print("no")

