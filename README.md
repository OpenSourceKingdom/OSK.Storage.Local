# OSK.Storage.Local

The projects are meant to provide a quick and useful way of saving and handling a variety of files via dependency injection. Through the various serializers, data can be saved using
JSON, YAML, binary, or any other format that integrates with the `ISerializer` interface. This project focuses on storage to a local device and does not
interact with cloud technologies. Additionally, through the use of dependency injection, various data manipulations, polymorphism, and other features can be added as needed for consumers.
 
# Local
This is the core logic of the project. It adds all the necessary services using the `AddLocalStorage` extension. 

The process for saving a file flows in the following:
* Caller calls the `ILocalStorageService` to save some data, using a set of save options
* Data is validated and then converted using one of the `ISerializer` objects within the dependency container
* After the data is converted into raw data from a serializer, the data is then processed through a list of `IRawDataProcessor` objects. These can manipulate the data further to allow for compression, encryption, etc.

The process for loadingn a file flows in the following:
* Call calls the `ILocalStorageService` to load some data
* Data is pulled from the location passed and ran through the list of `IRawDataProcessor` objects to reverse the data manipulations performed during saving.
* Data is then passed into the serializer to be converted into a useful object


# Snappier
Since data size can become an issue, snappier is a useful tool that helps to compress data quickly and efficiently in C#. You view more information and the library here https://github.com/brantburnett/Snappier
This project provides an integration to the `ILocalStorageService` to using this compression algorithm. By using the `AddLocalStorageSnappierCompression` extension, an `IRawDataProcessor` will be added to the dependency contaioner to be used during the local data manipulation processes prior to storage.

# Cryptography
For security needs, a cryptography integration exists that utilizes the `OSK.Security.Cryptography` codebase. By using the `AddLocalStorageCryptography` extension, the `ILocalStorageService` will gain access to a cryptographic data processor that handle encryption and decryption of data, using any of the
integrations for the cryptoraphic library. In order to fully utilize this extension, consumers will need to create an implementation for the `ICryptographicKeyRepository` and add it to the dependency container. This is used to retrieve the necessary key information
for the application to encrypt local data. Additionally, consumers should also add an implementation of a cryptographic algorithm so that it can be used for encryption/decryption

# Default Local Configuration
For consumers simply wanting access to a local storage service capable of handling binary, json, and yaml, without wanting to perform custom dependency setups, this project provides a quick convenience method to add
an implementation for these serializers for you, but it does add dependencies to the OSK serializers implementations, which is why the project is separated so consumers can decide to add those dependencies or not. By calling
`AddLocalStorageDefaultSerializers`, consumers will be able to setup the entirety of a `ILocalStorageService` dependency tree using the default configuration in the project.

# Default Polymorphism Configuration
In the even local storage is handling abstraction and other generic data, polymorhpism will be necessary in order to fully deserialized the objects into the correct types. This project provides a default setup with the `OSK.Serialization.Polymorphism` codebase 
and will setup an `Enum` based polymorphism strategy (see https://github.com/OpenSourceKingdom/OSK.Serialization.Polymorphism.Discriminators). To use this feature, consumers will want to use the
`AddLocalStorageDefaultPolymorphism` extension and provide an assembly type marker

# Contributions and Issues
Any and all contributions are appreciated! Please be sure to follow the branch naming convention OSK-{issue number}-{deliminated}-{branch}-{name} as current workflows rely on it for automatic issue closure. Please submit issues for discussion and tracking using the github issue tracker.