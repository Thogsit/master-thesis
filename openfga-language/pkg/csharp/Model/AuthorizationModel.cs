//
// OpenFGA/.NET SDK for OpenFGA
//
// API version: 1.x
// Website: https://openfga.dev
// Documentation: https://openfga.dev/docs
// Support: https://openfga.dev/community
// License: [Apache-2.0](https://github.com/openfga/dotnet-sdk/blob/main/LICENSE)
//
// NOTE: This file was auto generated. DO NOT EDIT.
//


using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenFga.Language.Model {
    /// <summary>
    /// AuthorizationModel
    /// </summary>
    [DataContract(Name = "AuthorizationModel")]
    public class AuthorizationModel : IEquatable<AuthorizationModel>, IValidatableObject {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationModel" /> class.
        /// </summary>
        [JsonConstructor]
        public AuthorizationModel() {
            AdditionalProperties = new Dictionary<string, object>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationModel" /> class.
        /// </summary>
        /// <param name="id">id (required).</param>
        /// <param name="schemaVersion">schemaVersion (required).</param>
        /// <param name="typeDefinitions">typeDefinitions (required).</param>
        /// <param name="conditions">conditions.</param>
        public AuthorizationModel(string id = null, string schemaVersion = null, List<TypeDefinition> typeDefinitions = null, Dictionary<string, Condition> conditions = null) {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            SchemaVersion = schemaVersion ?? throw new ArgumentNullException(nameof(schemaVersion));
            TypeDefinitions = typeDefinitions ?? throw new ArgumentNullException(nameof(typeDefinitions));
            Conditions = conditions;
            AdditionalProperties = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets or Sets Id
        /// </summary>
        [DataMember(Name = "id", IsRequired = true, EmitDefaultValue = false)]
        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or Sets SchemaVersion
        /// </summary>
        [DataMember(Name = "schema_version", IsRequired = true, EmitDefaultValue = false)]
        [JsonPropertyName("schema_version")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string SchemaVersion { get; set; }

        /// <summary>
        /// Gets or Sets TypeDefinitions
        /// </summary>
        [DataMember(Name = "type_definitions", IsRequired = true, EmitDefaultValue = false)]
        [JsonPropertyName("type_definitions")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public List<TypeDefinition> TypeDefinitions { get; set; }

        /// <summary>
        /// Gets or Sets Conditions
        /// </summary>
        [DataMember(Name = "conditions", EmitDefaultValue = false)]
        [JsonPropertyName("conditions")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Dictionary<string, Condition>? Conditions { get; set; }

        /// <summary>
        /// Gets or Sets additional properties
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties { get; set; }


        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson() {
            return JsonSerializer.Serialize(this);
        }

        /// <summary>
        /// Builds a AuthorizationModel from the JSON string presentation of the object
        /// </summary>
        /// <returns>AuthorizationModel</returns>
        public static AuthorizationModel FromJson(string jsonString) {
            return JsonSerializer.Deserialize<AuthorizationModel>(jsonString) ?? throw new InvalidOperationException();
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="input">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object input) {
            return this.Equals(input as AuthorizationModel);
        }

        /// <summary>
        /// Returns true if AuthorizationModel instances are equal
        /// </summary>
        /// <param name="input">Instance of AuthorizationModel to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(AuthorizationModel input) {
            if (input == null) {
                return false;
            }
            return
                (
                    this.Id == input.Id ||
                    (this.Id != null &&
                    this.Id.Equals(input.Id))
                ) &&
                (
                    this.SchemaVersion == input.SchemaVersion ||
                    (this.SchemaVersion != null &&
                    this.SchemaVersion.Equals(input.SchemaVersion))
                ) &&
                (
                    this.TypeDefinitions == input.TypeDefinitions ||
                    this.TypeDefinitions != null &&
                    input.TypeDefinitions != null &&
                    this.TypeDefinitions.SequenceEqual(input.TypeDefinitions)
                ) &&
                (
                    this.Conditions == input.Conditions ||
                    this.Conditions != null &&
                    input.Conditions != null &&
                    this.Conditions.SequenceEqual(input.Conditions)
                )
                && (this.AdditionalProperties.Count == input.AdditionalProperties.Count && !this.AdditionalProperties.Except(input.AdditionalProperties).Any());
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode() {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = 9661;
                if (this.Id != null) {
                    hashCode = (hashCode * 9923) + this.Id.GetHashCode();
                }
                if (this.SchemaVersion != null) {
                    hashCode = (hashCode * 9923) + this.SchemaVersion.GetHashCode();
                }
                if (this.TypeDefinitions != null) {
                    hashCode = (hashCode * 9923) + this.TypeDefinitions.GetHashCode();
                }
                if (this.Conditions != null) {
                    hashCode = (hashCode * 9923) + this.Conditions.GetHashCode();
                }
                if (this.AdditionalProperties != null) {
                    hashCode = (hashCode * 9923) + this.AdditionalProperties.GetHashCode();
                }
                return hashCode;
            }
        }

        /// <summary>
        /// To validate all properties of the instance
        /// </summary>
        /// <param name="validationContext">Validation context</param>
        /// <returns>Validation Result</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            yield break;
        }

    }

}